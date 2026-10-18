# Outbox message lifecycle

A map of the states an outbox message goes through. For setup and the API, see [Transactional Outbox](README.md#transactional-outbox).

A message is one row of [`OutboxMessage`](Outbox/Models/OutboxMessage.cs).

## The states

### 1. Getting into the queue

[`IOutbox<TDbContext>`](Outbox/Outbox%7BTDbContext%7D.cs):

```mermaid
stateDiagram-v2
    direction TB

    state "Tracked<br/><small>in your change tracker, no row yet</small>" as Tracked
    state "Scheduled<br/><small>AvailableAt in the future</small>" as Scheduled
    state "Due<br/><small>AvailableAt reached, waits for a processor</small>" as Due
    state "never written" as Discarded
    state "row deleted" as Removed
    state "Delivery<br/><small>next diagram</small>" as Delivery

    [*] --> Tracked : (1.) AddOnSave
    [*] --> Due : (5.) AddNowAsync
    [*] --> Scheduled : (6.) AddNowAsync, later AvailableAt
    Tracked --> Discarded : (2.) rollback or RemoveOnSaveAsync
    Tracked --> Due : (3.) your SaveChangesAsync commits
    Tracked --> Scheduled : (4.) commits, later AvailableAt
    Scheduled --> Due : (7.) AvailableAt reached
    Scheduled --> Scheduled : (8.) Postpone…Async
    Due --> Scheduled : (8.) Postpone…Async
    Scheduled --> Removed : (9.) Remove…Async
    Due --> Removed : (9.) Remove…Async
    Due --> Delivery : (10.) a processor claims it

    classDef gone stroke-dasharray:5 4
    class Discarded,Removed,Delivery gone
```

### 2. Delivery

[`OutboxProcessor<TDbContext>`](Outbox/OutboxProcessor%7BTDbContext%7D.cs)

A reference to a message is called a *ticket* (id + concurrency stamp). Once anything claims a message, the messages' tickets become stale.

```mermaid
stateDiagram-v2
    direction TB

    state "Due<br/><small>AvailableAt reached, waits for a processor</small>" as Due
    state "In flight<br/><small>claimed, lease running, handler busy</small>" as InFlight
    state "Backing off<br/><small>failed, waits for its retry delay</small>" as Backoff
    state "Abandoned<br/><small>lease expired, no outcome recorded</small>" as Abandoned
    state "Processed<br/><small>ProcessedAt set, terminal</small>" as Processed
    state "Dead-lettered<br/><small>FailedAt set, terminal, kept forever</small>" as DeadLettered
    state "row deleted" as Deleted

    [*] --> Due : from enqueueing (3., 5., 7.)
    Due --> InFlight : (10.) claimed, Attempts + 1<br/><small>OutboxMessage.Claim</small>
    Due --> DeadLettered : (11.) row cannot be read<br/><small>DeadLetterUnreadableAsync</small>
    InFlight --> Processed : (12.) handler returned<br/><small>MarkProcessed</small>
    InFlight --> Backoff : (13.) handler threw, retry left<br/><small>MarkRetrying</small>
    InFlight --> DeadLettered : (14.) handler threw, no retry left<br/><small>MarkFailed</small>
    InFlight --> Abandoned : (15.) no outcome before the lease expired
    Backoff --> Due : (16.) retry delay elapsed
    Abandoned --> InFlight : (17.) retry left, claimed again<br/><small>OutboxMessage.Claim</small>
    Abandoned --> DeadLettered : (18.) no retry left<br/><small>DeadLetterAbandonedAsync</small>
    Processed --> Deleted : (19.) older than Retention<br/><small>ApplyRetentionAsync</small>
    DeadLettered --> Deleted : (20.) deleted by hand
    Deleted --> [*]

    classDef terminal stroke-width:3px
    classDef gone stroke-dasharray:5 4
    class Processed,DeadLettered terminal
    class Deleted gone
```

## State legend

| State | The row | Where to look |
|---|---|---|
| **Tracked** | No row yet, only an `Added` entry in your context | `Outbox.AddOnSave` |
| **Scheduled** | `AvailableAt` > now | `OutboxMessageSettings.AvailableAt`, `Outbox.Postpone…Async` |
| **Due** | `AvailableAt` ≤ now, unclaimed | `OutboxProcessor.CreateDeliverableMessagesQuery` |
| **In flight** | Claimed, `ClaimedUntil` > now | `OutboxMessage.Claim` |
| **Backing off** | `AvailableAt` > now, `LastError` set | `OutboxMessage.MarkRetrying`, `OutboxOptions.RetryDelays` |
| **Abandoned** | Claimed, `ClaimedUntil` < now | `OutboxMessage.IsAbandonedWithoutRetry` |
| **Processed** | `ProcessedAt` set | `OutboxMessage.MarkProcessed`, `OutboxOptions.Retention` |
| **Dead-lettered** | `FailedAt` set | `OutboxMessage.MarkFailed`, [below](#dead-letters) |

## One processor run

`ProcessPendingAsync` handles one batch per call. The numbers in brackets refer to the transitions above.

```mermaid
flowchart TD
    start(["ProcessPendingAsync<br/><small>poll, trigger or your scheduler</small>"]) --> read["Read up to BatchSize due rows of registered types,<br/>oldest AvailableAt first<br/><small>GetDeliverableAsync</small>"]
    read --> readable{"Whole batch<br/>readable?"}
    readable -- "no" --> rowByRow["Re-read the same batch row by row<br/><small>GetDeliverableRowByRowAsync</small>"]
    rowByRow --> unreadable["Dead-letter each unreadable row (11.)<br/><small>DeadLetterUnreadableAsync</small>"]
    unreadable --> next
    readable -- "yes" --> next{"Next message?<br/><small>stops early when cancelled</small>"}

    next -- "none left" --> retention["Delete Processed rows older than Retention (19.)<br/><small>ApplyRetentionAsync</small>"]
    retention --> done(["Return the number delivered"])

    next -- "message" --> abandoned{"Lease expired and<br/>Attempts > RetryDelays.Count?<br/><small>IsAbandonedWithoutRetry</small>"}
    abandoned -- "yes" --> deadAbandoned["Dead-letter (18.)<br/><small>DeadLetterAbandonedAsync</small>"]
    deadAbandoned --> next
    abandoned -- "no" --> claim["Claim: Attempts + 1, lease, new stamp (10., 17.)<br/><small>TryClaimAsync → OutboxMessage.Claim</small>"]
    claim --> won{"Claim saved?"}
    won -- "no, another processor won<br/><small>DbUpdateConcurrencyException</small>" --> next
    won -- "yes" --> dispatch["Deserialize and call the handler<br/><small>OutboxRegistration.Dispatch → IOutboxMessageHandler.HandleAsync</small>"]

    dispatch -- "returned" --> processed["Processed (12.)<br/><small>MarkProcessed</small>"]
    processed --> next
    dispatch -- "threw" --> retryLeft{"Attempts ≤<br/>RetryDelays.Count?"}
    retryLeft -- "yes" --> retrying["Backing off (13.)<br/><small>MarkRetrying: AvailableAt = now + RetryDelays[Attempts - 1]</small>"]
    retrying --> next
    retryLeft -- "no" --> failed["Dead-lettered (14.)<br/><small>MarkFailed</small>"]
    failed --> next
    dispatch -- "cancelled<br/><small>shutdown or Timeout, whatever it threw</small>" --> cancelled(["Throw. The lease stays and expires on its own (15.)<br/><small>the rest of the batch stays Due, no retention this run</small>"])
```
