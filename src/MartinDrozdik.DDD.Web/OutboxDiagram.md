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
    Due --> Delivery : (10.) a processor reserves it

    classDef gone stroke-dasharray:5 4
    class Discarded,Removed,Delivery gone
```

### 2. Delivery

[`OutboxProcessor<TDbContext>`](Outbox/OutboxProcessor%7BTDbContext%7D.cs)

A reference to a message is called a *ticket* (id + concurrency stamp). Once a processor claims the message, its tickets become stale (new concurrency token).

```mermaid
stateDiagram-v2
    direction TB

    state "Due<br/><small>AvailableAt reached, nobody holds it</small>" as Due
    state "Reserved<br/><small>in a batch, waits for its turn, no attempt yet</small>" as Reserved
    state "In flight<br/><small>claimed, lease running, handler busy</small>" as InFlight
    state "Backing off<br/><small>failed, waits for its retry delay</small>" as Backoff
    state "Abandoned<br/><small>lease expired, no outcome recorded</small>" as Abandoned
    state "Processed<br/><small>ProcessedAt set, terminal</small>" as Processed
    state "Dead-lettered<br/><small>FailedAt set, terminal, kept forever</small>" as DeadLettered
    state "row deleted" as Deleted

    [*] --> Due : from enqueueing (3., 5., 7.)
    Due --> Reserved : (10.) reserved with its batch<br/><small>ReserveAsync</small>
    Reserved --> Due : (11.) reservation expired, or released at the end of the run<br/><small>ReleaseReservationsAsync</small>
    Reserved --> DeadLettered : (12.) row cannot be read<br/><small>DeadLetterUnreadableAsync</small>
    Reserved --> InFlight : (13.) claimed while the reservation stands, Attempts + 1<br/><small>OutboxMessage.Claim</small>
    InFlight --> Processed : (14.) handler returned<br/><small>MarkProcessed</small>
    InFlight --> Backoff : (15.) handler threw, retry left<br/><small>MarkRetrying</small>
    InFlight --> DeadLettered : (16.) handler threw, no retry left<br/><small>MarkFailed</small>
    InFlight --> Abandoned : (17.) no outcome before the lease expired
    Backoff --> Due : (18.) retry delay elapsed
    Abandoned --> Reserved : (19.) reserved with a later batch
    Reserved --> DeadLettered : (20.) abandoned, no retry left<br/><small>DeadLetterAbandonedAsync</small>
    Processed --> Deleted : (21.) older than Retention<br/><small>IOutboxRetention.ApplyAsync, in chunks</small>
    DeadLettered --> Deleted : (22.) deleted by hand
    Abandoned --> Processed : (23.) the delivery that outlived its lease returned<br/><small>RecordLateSuccessAsync</small>
    Backoff --> Processed : (23.) the delivery that outlived its lease returned
    DeadLettered --> Processed : (23.) the delivery that outlived its lease returned
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
| **Due** | `AvailableAt` ≤ now, neither reserved nor claimed | `OutboxMessage.IsDueAndFree` |
| **Reserved** | `ClaimedBy` set, `ReservedUntil` > now | `OutboxProcessor.ReserveAsync`, `OutboxOptions.ReservationDuration` |
| **In flight** | Claimed, `ClaimedUntil` > now | `OutboxMessage.Claim`, `OutboxOptions.LeaseDuration` |
| **Backing off** | `AvailableAt` > now, `LastError` set | `OutboxMessage.MarkRetrying`, `OutboxOptions.RetryDelays` |
| **Abandoned** | Claimed, `ClaimedUntil` < now | `OutboxMessage.IsAbandonedWithoutRetry` |
| **Processed** | `ProcessedAt` set | `OutboxMessage.MarkProcessed`, `OutboxRetentionOptions.Retention` |
| **Dead-lettered** | `FailedAt` set | `OutboxMessage.MarkFailed`, [below](#dead-letters) |

## One processor run

`ProcessPendingAsync` handles one batch per call. `OutboxDispatchRecurringTask` calls it again while `OutboxBatchResult.MayHaveMore`, so one iteration drains a backlog. The numbers in brackets refer to the transitions above.

```mermaid
flowchart TD
    start(["ProcessPendingAsync<br/><small>poll, trigger or your scheduler</small>"]) --> reserve["Reserve up to BatchSize due, free rows of registered types,<br/>oldest AvailableAt first (10.)<br/><small>ReserveAsync: read the ids, then one conditional update</small>"]
    reserve --> read["Read back the rows this run reserved, by those ids<br/><small>GetReservedAsync</small>"]
    read --> readable{"Whole batch<br/>readable?"}
    readable -- "no" --> rowByRow["Re-read the same batch row by row<br/><small>GetRowByRowAsync</small>"]
    rowByRow --> unreadable["Dead-letter each unreadable row (12.)<br/><small>DeadLetterUnreadableAsync</small>"]
    unreadable --> next
    readable -- "yes" --> next{"Next message?<br/><small>stops early when cancelled</small>"}

    next -- "none left" --> release["Release the reservations not claimed (11.)<br/><small>ReleaseReservationsAsync, however the run ended</small>"]
    release --> done(["Return OutboxBatchResult<br/><small>found, reserved, released, delivered, MayHaveMore</small>"])

    next -- "message" --> stands{"Reservation still stands?<br/><small>OutboxMessage.IsReservedBy</small>"}
    stands -- "no, an earlier message outlasted it" --> skipped["Skip it, it belongs to whoever reserves it next (11.)"]
    skipped --> next
    stands -- "yes" --> abandoned{"Lease expired and<br/>Attempts > RetryDelays.Count?<br/><small>IsAbandonedWithoutRetry</small>"}
    abandoned -- "yes" --> deadAbandoned["Dead-letter (20.)<br/><small>DeadLetterAbandonedAsync</small>"]
    deadAbandoned --> next
    abandoned -- "no" --> claim["Claim: Attempts + 1, full lease, new stamp (13.)<br/><small>TryClaimAsync → OutboxMessage.Claim</small>"]
    claim --> won{"Claim saved?"}
    won -- "no, the stamp changed<br/><small>DbUpdateConcurrencyException</small>" --> next
    won -- "yes" --> dispatch["Deserialize and call the handler<br/><small>OutboxRegistration.Dispatch → IOutboxMessageHandler.HandleAsync</small>"]

    dispatch -- "returned" --> processed["Processed (14.)<br/><small>MarkProcessed</small>"]
    processed --> saved{"Saved?"}
    saved -- "yes" --> next
    saved -- "no, the lease expired and another processor took it<br/><small>DbUpdateConcurrencyException</small>" --> late["Record it anyway, unless already processed (23.)<br/><small>RecordLateSuccessAsync: conditional update, new stamp</small>"]
    late --> next
    dispatch -- "threw" --> retryLeft{"Attempts ≤<br/>RetryDelays.Count?"}
    retryLeft -- "yes" --> retrying["Backing off (15.)<br/><small>MarkRetrying: AvailableAt = now + RetryDelays[Attempts - 1]</small>"]
    retrying --> next
    retryLeft -- "no" --> failed["Dead-lettered (16.)<br/><small>MarkFailed</small>"]
    failed --> next
    dispatch -- "cancelled<br/><small>shutdown or Timeout, whatever it threw</small>" --> cancelled(["Throw. The lease stays and expires on its own (17.)<br/><small>the reservations not claimed are released</small>"])
```

Two checks prevent a double delivery. The update that reserves the batch repeats the "due and free" filter, and the database checks it again on each row it locks. A row another processor reserved, claimed or postponed after the ids were read is skipped. Without that check, the read-back would pick up the stamp of the other processor's claim, and this run's claim would succeed as well. The claim is saved against the concurrency stamp, so of two processors holding the same stamp, only one claims.

A handler that runs past `LeaseDuration` can be delivered twice: the message is taken again once the lease expires. When the slow delivery returns, its success is recorded anyway (23.), over a retry or a dead letter recorded in the meantime. Only an earlier recorded delivery is kept.
