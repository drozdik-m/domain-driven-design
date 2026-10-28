using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Delivers pending messages held in <typeparamref name="TDbContext"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="context">The context the messages are read from and written back to; the processor has it to itself and clears its change tracker before every batch.</param>
/// <param name="registry">The message types of this outbox; nothing else is read, claimed or delivered.</param>
/// <param name="scopeFactory">Creates the scope each handler is resolved from.</param>
/// <param name="options">Options driving batching, leases, retries and serialization.</param>
/// <param name="timeProvider">Source of the current instant.</param>
/// <param name="logger">Logger for delivery outcomes.</param>
internal sealed class OutboxProcessor<TDbContext>(
    TDbContext context,
    OutboxRegistry<TDbContext> registry,
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions<TDbContext>> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TDbContext>> logger) : IOutboxProcessor<TDbContext>
    where TDbContext : DbContext
{
    private static readonly string s_outboxName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public async Task<OutboxBatchResult> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        // Start every batch from an empty change tracker
        // Prevents stale entities fucking up the flow.
        // Keeps the tracker small while the dispatch task drains batch after batch in one scope.
        context.ChangeTracker.Clear();

        var claimId = Guid.CreateVersion7(); // A distinct claim id per batch
        var (candidates, reserved) = await ReserveAsync(claimId, cancellationToken);

        var released = 0;
        var dispatched = 0;
        if (reserved > 0)
        {
            try
            {
                var batch = await GetReservedAsync(claimId, candidates, cancellationToken);
                dispatched = await DeliverAsync(batch, claimId, cancellationToken);
            }
            finally
            {
                released = await ReleaseReservationsAsync(claimId, candidates);
            }

            OutboxLogging.LogBatchCompleted(logger, s_outboxName, dispatched, reserved);
        }

        return new OutboxBatchResult(candidates.Count, reserved, released, dispatched);
    }

    /// <summary>
    /// Delivers the reserved messages of a batch one by one.
    /// </summary>
    /// <param name="batch">The reserved messages, the longest waiting first.</param>
    /// <param name="claimId">The identity of this run.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The number of messages delivered.</returns>
    private async Task<int> DeliverAsync(IReadOnlyList<OutboxMessage> batch, Guid claimId, CancellationToken cancellationToken)
    {
        var dispatched = 0;
        foreach (var message in batch)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Never claim what this instance cannot deliver
            if (!registry.TryGet(message.MessageType, out var registration))
            {
                continue;
            }

            // An earlier message of the batch may have outlasted the reservation, then this one belongs to whoever reserves it next
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (!message.IsReservedBy(claimId, now))
            {
                OutboxLogging.LogReservationExpired(logger, s_outboxName, message.Id);
                continue;
            }

            // This message has been attempted too many times -> dead-letter
            if (message.IsAbandonedWithoutRetry(now, options.Value.RetryDelays.Count))
            {
                await DeadLetterAbandonedAsync(message, cancellationToken);
                continue;
            }

            // Take a lease on the message to process it
            if (!await TryClaimAsync(message, claimId, cancellationToken))
            {
                continue;
            }

            // Try to do the actual delivery and record the outcome
            // The result is saved even when cancellation is requested, so the message is not double-delivered
            if (await TryDeliverAsync(message, registration, cancellationToken))
            {
                dispatched++;
            }
        }

        return dispatched;
    }

    /// <summary>
    /// Reserves the next batch for this run.
    /// Processors running at the same time read past these messages.
    /// </summary>
    /// <remarks>
    /// Reads the ids of the next batch, then reserves them in one conditional update.
    /// Counts no attempt.
    /// Does not rotate the concurrency stamp.
    /// </remarks>
    /// <param name="claimId">The identity of this run.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The ids read for the batch, and how many of them this run reserved.</returns>
    private async Task<(IReadOnlyList<Guid> Candidates, int Reserved)> ReserveAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reservedUntil = now + options.Value.ReservationDuration;
        var messageTypes = registry.Registrations
            .Select(e => e.MessageType)
            .ToList();

        // Ordered by the indexed AvailableAt - not every database sorts a v7 GUID by its time part.
        // SQL Server compares the last bytes first so an order that ignores waiting time could starve old messages
        // Ignores unknown message types
        var candidates = await context.Set<OutboxMessage>()
            .Where(OutboxMessage.IsDueAndFree(now))
            .Where(m => messageTypes.Contains(m.MessageType))
            .OrderBy(m => m.AvailableAt)
            .ThenBy(m => m.Id)
            .Take(options.Value.BatchSize)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return ([], 0);
        }

        // Another processor may have reserved, claimed or postponed a candidate since it was read.
        // The database re-checks the IsDueAndFree condition on the locked row and does atomic update.
        var reserved = await context.Set<OutboxMessage>()
            .Where(OutboxMessage.IsDueAndFree(now)) // Re-check the condition on the locked row
            .Where(m => candidates.Contains(m.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.ClaimedBy, claimId)
                    .SetProperty(m => m.ReservedUntil, reservedUntil),
                cancellationToken);

        return (candidates, reserved);
    }

    /// <summary>
    /// Reads the messages this run reserved.
    /// </summary>
    /// <remarks>
    /// A row that cannot be read is dead-lettered, see <see cref="GetRowByRowAsync"/>.
    /// </remarks>
    /// <param name="claimId">The identity of this run.</param>
    /// <param name="candidates">The ids read for the batch; another processor may have reserved some of them first.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The messages to attempt, the longest waiting first.</returns>
    private async Task<IReadOnlyList<OutboxMessage>> GetReservedAsync(Guid claimId, IReadOnlyList<Guid> candidates, CancellationToken cancellationToken)
    {
        // Get reserved by this batch
        var reserved = context.Set<OutboxMessage>()
            .Where(m => candidates.Contains(m.Id) && m.ClaimedBy == claimId && m.ReservedUntil != null)
            .OrderBy(m => m.AvailableAt)
            .ThenBy(m => m.Id);

        try
        {
            return await reserved.ToListAsync(cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is ArgumentException)
        {
            // The content is unparsable, e.g. a broken invariant, and the whole batch cannot be read
            // Read row by row and deadletter the broken messages
            return await GetRowByRowAsync(reserved, cancellationToken);
        }
    }

    /// <summary>
    /// Fallback way of reading a batch if reading the whole batch failed.
    /// Reads a batch one row at a time and dead-letters every row that cannot be read.
    /// </summary>
    /// <param name="batchQuery">The query of the batch.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The readable messages of the batch, the longest waiting first.</returns>
    private async Task<IReadOnlyList<OutboxMessage>> GetRowByRowAsync(IQueryable<OutboxMessage> batchQuery, CancellationToken cancellationToken)
    {
        // Query by ID - reading that won't fail invariants etc.
        var ids = await batchQuery
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var messages = new List<OutboxMessage>(ids.Count);
        foreach (var id in ids)
        {
            try
            {
                // Rows read before the batch failed are tracked and found from the change tracker
                if (await context.Set<OutboxMessage>().FindAsync([id], cancellationToken) is { } message)
                {
                    messages.Add(message);
                }
            }
            catch (InvalidOperationException exception) when (exception.InnerException is ArgumentException rejected)
            {
                // Found the invalid row, dead-letter it and continue
                await DeadLetterUnreadableAsync(id, rejected, cancellationToken);
            }
        }

        return messages;
    }

    /// <summary>
    /// Releases the messages this run reserved but did not claim, so other processors may take them at once.
    /// </summary>
    /// <remarks>
    /// Runs however the batch ended, regardless of any cancellation.
    /// A failure is only logged: an unreleased reservation expires on its own.
    /// </remarks>
    /// <param name="claimId">The identity of this run.</param>
    /// <param name="candidates">The ids read for the batch.</param>
    /// <returns>The number of messages released, zero when releasing failed.</returns>
    private async Task<int> ReleaseReservationsAsync(Guid claimId, IReadOnlyList<Guid> candidates)
    {
        try
        {
            // A reservation that expired and was already taken by another run has that run's claim, and is left alone
            return await context.Set<OutboxMessage>()
                .Where(m => candidates.Contains(m.Id) && m.ClaimedBy == claimId && m.ReservedUntil != null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(m => m.ClaimedBy, (Guid?)null)
                        .SetProperty(m => m.ReservedUntil, (DateTime?)null),
                    CancellationToken.None);
        }
        catch (Exception exception)
        {
            // Reservations expire, just continue
            OutboxLogging.LogReservationsNotReleased(logger, exception, s_outboxName, claimId);
            return 0;
        }
    }

    /// <summary>
    /// Dead-letters a row that cannot be read, without attempting it.
    /// </summary>
    /// <remarks>
    /// May be caused by broken invariants.
    /// Written straight to the table.
    /// </remarks>
    /// <param name="id">The identity of the row.</param>
    /// <param name="exception">Why the row cannot be read.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the outcome has been stored.</returns>
    private async Task DeadLetterUnreadableAsync(Guid id, ArgumentException exception, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var error = OutboxMessage.Truncate($"The row could not be read, it can never be delivered: {exception.Message}");
        var rotatedConcurrencyStamp = Guid.CreateVersion7();

        // Multiple instances may try to dead-letter the same row, only one will succeed
        var deadLettered = await context.Set<OutboxMessage>()
            .Where(e => e.Id == id && e.ProcessedAt == null && e.FailedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.FailedAt, now)
                    .SetProperty(m => m.LastError, error)
                    .SetProperty(m => m.ClaimedBy, (Guid?)null)
                    .SetProperty(m => m.ClaimedUntil, (DateTime?)null)
                    .SetProperty(m => m.ReservedUntil, (DateTime?)null)
                    .SetProperty(m => m.ConcurrencyStamp, rotatedConcurrencyStamp),
                cancellationToken);

        // Someone else may have already dead-lettered it
        if (deadLettered > 0)
        {
            OutboxLogging.LogUnreadableDeadLettered(logger, exception, s_outboxName, id);
        }
    }

    /// <summary>
    /// Dead-letters a message whose last attempts' lease expired, without attempting it again.
    /// </summary>
    /// <param name="message">The message whose attempts have run out.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the outcome has been stored.</returns>
    private async Task DeadLetterAbandonedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        message.MarkFailed(now, $"Attempt {message.Attempts} recorded no outcome before its lease expired: it was cut short (cancelled, e.g. by the timeout of the dispatch task, or its process stopped) or outlived the lease. No retry is left.");

        if (await TrySaveAsync(message, cancellationToken))
        {
            OutboxLogging.LogAbandonedDeadLettered(logger, s_outboxName, message.Id, message.MessageType.Key, message.Attempts);
        }
    }

    /// <summary>
    /// Takes a lease on a message so no other processor delivers it at the same time.
    /// </summary>
    /// <param name="message">The message to claim.</param>
    /// <param name="claimId">The identity of this run.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>True when the claim was taken, false when another processor won the race.</returns>
    private Task<bool> TryClaimAsync(OutboxMessage message, Guid claimId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        message.Claim(claimId, now, options.Value.LeaseDuration);

        return TrySaveAsync(message, cancellationToken);
    }

    /// <summary>
    /// Delivers a claimed message and records the outcome.
    /// </summary>
    /// <remarks>
    /// Once an attempt has happened (dispatch finished), its outcome is saved even when <paramref name="cancellationToken"/> is cancelled (save it rather than double-deliver).
    /// </remarks>
    /// <param name="message">The claimed message.</param>
    /// <param name="registration">The registration of the message type, which delivers it to its handler.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>True when the message was delivered, else false.</returns>
    private async Task<bool> TryDeliverAsync(OutboxMessage message, OutboxRegistration registration, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await registration.Dispatch(scope.ServiceProvider, message.Payload, options.Value.SerializerOptions, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down, or over the timeout of the run... The attempt counter was already increment.
            // The lease is left in place and expires on its own; the message is attempted again, or dead-lettered when no retry is left.
            throw;
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested)
        {
            // Handlers may report the cancellation as their own exception (SqlException, DbUpdateException, a wrapping exception, ...).
            // Rethrown as cancellation so callers can tell a timeout from a failure.
            OutboxLogging.LogDeliveryInterrupted(logger, exception, s_outboxName, message.Id, message.MessageType.Key, message.Attempts);
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (Exception exception)
        {
            // The delivery failed for any reason
            // Record the failure and schedule a retry, or dead-letter when no retry is left
            await RecordFailureAsync(message, exception);
            return false;
        }

        // Record the success
        // Do it even when cancellation is requested, so the message is not double-delivered
        var processedAt = timeProvider.GetUtcNow().UtcDateTime;
        message.MarkProcessed(processedAt);
        MarkClaimColumnsModified(message);
        try
        {
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The lease expired during the delivery and another processor took the message meanwhile
            // "Force-save" the current success
            context.Entry(message).State = EntityState.Detached;
            return await RecordLateSuccessAsync(message, processedAt);
        }

        OutboxLogging.LogDispatched(logger, s_outboxName, message.Id, message.MessageType.Key, message.Attempts);
        return true;
    }

    /// <summary>
    /// Records a delivery that finished after its lease expired.
    /// Another processor may have already retried, force the current success to be the outcome.
    /// </summary>
    /// <remarks>
    /// Very rare but possible.
    /// <para>
    /// The rotated stamp rejects the outcome of an attempt still in flight, so it cannot schedule a retry or dead-letter the message afterwards.
    /// </para>
    /// </remarks>
    /// <param name="message">The delivered message, no longer tracked.</param>
    /// <param name="processedAt">The UTC instant the delivery finished.</param>
    /// <returns>True when the delivery was recorded, false when the message was already delivered or is gone.</returns>
    private async Task<bool> RecordLateSuccessAsync(OutboxMessage message, DateTime processedAt)
    {
        var id = message.Id;
        var rotatedConcurrencyStamp = Guid.CreateVersion7();
        var recorded = await context.Set<OutboxMessage>()
            .Where(m => m.Id == id && m.ProcessedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.ProcessedAt, processedAt)
                    .SetProperty(m => m.FailedAt, (DateTime?)null)
                    .SetProperty(m => m.LastError, (string?)null)
                    .SetProperty(m => m.ClaimedBy, (Guid?)null)
                    .SetProperty(m => m.ClaimedUntil, (DateTime?)null)
                    .SetProperty(m => m.ReservedUntil, (DateTime?)null)
                    .SetProperty(m => m.ConcurrencyStamp, rotatedConcurrencyStamp),
                CancellationToken.None);

        if (recorded == 0)
        {
            OutboxLogging.LogClaimLost(logger, s_outboxName, id);
            return false;
        }

        OutboxLogging.LogDeliveredAfterLeaseExpired(logger, s_outboxName, id, message.MessageType.Key, options.Value.LeaseDuration);
        return true;
    }

    /// <summary>
    /// Schedules the next attempt, or dead-letters the message when the retry delays run out.
    /// </summary>
    /// <remarks>
    /// Saved regardless of any cancellation.
    /// </remarks>
    /// <param name="message">The message that failed.</param>
    /// <param name="exception">The failure that prevented delivery.</param>
    /// <returns>A <see cref="Task"/> that completes when the outcome has been stored.</returns>
    private Task<bool> RecordFailureAsync(OutboxMessage message, Exception exception)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var retryDelays = options.Value.RetryDelays;

        // The attempt is already incremented at this time (at claim time)
        var attemptIndex = message.Attempts - 1;

        if (attemptIndex < retryDelays.Count)
        {
            var delay = retryDelays[attemptIndex];
            message.MarkRetrying(now, delay, exception.ToString());
            OutboxLogging.LogRetrying(logger, exception, s_outboxName, message.Id, message.MessageType.Key, message.Attempts, delay);
        }
        else
        {
            message.MarkFailed(now, exception.ToString());
            OutboxLogging.LogDeadLettered(logger, exception, s_outboxName, message.Id, message.MessageType.Key, message.Attempts);
        }

        MarkClaimColumnsModified(message);
        return TrySaveAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Makes sure saving an outcome clears the claim and reservation columns.
    /// </summary>
    /// <remarks>
    /// EF Core only writes columns that changed since the message was loaded.
    /// But when a slow delivery outlives its lease, another processor may reserve the row in the meantime.
    /// Without this, that reservation would stay on the row and a retry would wait for it to expire.
    /// </remarks>
    /// <param name="message">The message whose outcome is about to be saved.</param>
    private void MarkClaimColumnsModified(OutboxMessage message)
    {
        var entry = context.Entry(message);
        entry.Property(m => m.ClaimedBy).IsModified = true;
        entry.Property(m => m.ClaimedUntil).IsModified = true;
        entry.Property(m => m.ReservedUntil).IsModified = true;
    }

    /// <summary>
    /// Saves a change to a message.
    /// Treats a lost concurrency race as an ordinary outcome.
    /// </summary>
    /// <param name="message">The message being written.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>True when the change was stored, false when another processor got there first.</returns>
    private async Task<bool> TrySaveAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another processor changed the row first.
            // Stop tracking our stale copy so the rest of the batch can still be saved, and leave the message to whoever owns it now.
            OutboxLogging.LogClaimLost(logger, s_outboxName, message.Id);
            context.Entry(message).State = EntityState.Detached;
            return false;
        }
    }
}
