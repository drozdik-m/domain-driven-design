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
/// <param name="context">The context the messages are read from and written back to.</param>
/// <param name="registry">The message types of this outbox; nothing else is read, claimed or delivered.</param>
/// <param name="scopeFactory">Creates the scope each handler is resolved from.</param>
/// <param name="options">Options driving batching, leases, retries, retention and serialization.</param>
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
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await GetDeliverableAsync(cancellationToken);

        // A distinct claim per batch, so a lease can be traced back to the run that took it.
        var claimId = Guid.CreateVersion7();
        var claimed = 0;
        var dispatched = 0;

        foreach (var message in pending)
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

            if (message.IsAbandonedWithoutRetry(timeProvider.GetUtcNow().UtcDateTime, options.Value.RetryDelays.Count))
            {
                await DeadLetterAbandonedAsync(message, cancellationToken);
                continue;
            }

            if (!await TryClaimAsync(message, claimId, cancellationToken))
            {
                continue;
            }

            claimed++;
            if (await TryDeliverAsync(message, registration, cancellationToken))
            {
                dispatched++;
            }
        }

        if (claimed > 0)
        {
            OutboxLogging.LogBatchCompleted(logger, s_outboxName, dispatched, claimed);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return dispatched;
        }

        await ApplyRetentionAsync(cancellationToken);

        return dispatched;
    }

    /// <summary>
    /// Reads the messages that may be delivered right now.
    /// </summary>
    /// <remarks>
    /// Ignores unknown message types.
    /// A row that cannot be read is dead-lettered, see <see cref="GetDeliverableRowByRowAsync"/>.
    /// </remarks>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The messages to attempt, the longest waiting first.</returns>
    private async Task<IReadOnlyList<OutboxMessage>> GetDeliverableAsync(CancellationToken cancellationToken)
    {
        var deliverable = CreateDeliverableMessagesQuery();

        try
        {
            return await deliverable.ToListAsync(cancellationToken);
        }

        // A row is unreadable when the rules of the model reject a stored value, e.g. a blank payload
        catch (InvalidOperationException exception) when (exception.InnerException is ArgumentException)
        {
            return await GetDeliverableRowByRowAsync(deliverable, cancellationToken);
        }
    }

    /// <summary>
    /// Builds the query for the messages that may be delivered right now.
    /// </summary>
    /// <returns>The query of the batch, the longest waiting first.</returns>
    private IQueryable<OutboxMessage> CreateDeliverableMessagesQuery()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var messageTypes = registry.Registrations
            .Select(e => e.MessageType)
            .ToList();

        // Ordered by the indexed AvailableAt - not every database sorts a v7 GUID by its time part
        // (SQL Server compares the last bytes first), and an order that ignores waiting time could starve old messages
        return context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null &&
                m.FailedAt == null &&
                m.AvailableAt <= now &&
                (m.ClaimedUntil == null || m.ClaimedUntil < now) &&
                messageTypes.Contains(m.MessageType))
            .OrderBy(m => m.AvailableAt)
            .ThenBy(m => m.Id)
            .Take(options.Value.BatchSize);
    }

    /// <summary>
    /// Fallback way of reading a batch if reading the whole batch failed.
    /// Reads a batch one row at a time and dead-letters every row that cannot be read.
    /// </summary>
    /// <param name="deliverable">The query of the batch.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The readable messages of the batch, the longest waiting first.</returns>
    private async Task<IReadOnlyList<OutboxMessage>> GetDeliverableRowByRowAsync(IQueryable<OutboxMessage> deliverable, CancellationToken cancellationToken)
    {
        // Query by ID - reading that won't fail invariants etc.
        var ids = await deliverable
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var messages = new List<OutboxMessage>(ids.Count);
        foreach (var id in ids)
        {
            try
            {
                // Rows read before the batch failed are tracked already and served from the change tracker
                if (await context.Set<OutboxMessage>().FindAsync([id], cancellationToken) is { } message)
                {
                    messages.Add(message);
                }
            }
            catch (InvalidOperationException exception) when (exception.InnerException is ArgumentException rejected)
            {
                await DeadLetterUnreadableAsync(id, rejected, cancellationToken);
            }
        }

        return messages;
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
    /// Once an attempt has happened, its outcome is saved even when <paramref name="cancellationToken"/> is cancelled (save it rather than double-deliver).
    /// An attempt that throws after <paramref name="cancellationToken"/> was cancelled is cut short, whatever the exception, and records no outcome.
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
            // Shutting down, or over the timeout of the run... The attempt was counted by the claim.
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
#pragma warning disable CA1031 // Do not catch general exception types - any handler failure is a retryable delivery failure
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await RecordFailureAsync(message, exception);
            return false;
        }

        message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
        if (!await TrySaveAsync(message, CancellationToken.None))
        {
            return false;
        }

        OutboxLogging.LogDispatched(logger, s_outboxName, message.Id, message.MessageType.Key, message.Attempts);
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

        // The failed attempt was counted by its claim
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

        return TrySaveAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Saves a change to a message, treating a lost concurrency race as an ordinary outcome.
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

    /// <summary>
    /// Deletes delivered messages that are older than the configured retention.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when old messages have been removed.</returns>
    private async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Retention is not { } retention)
        {
            return;
        }

        // Dead-lettered messages are deliberately kept - they are the ones somebody has to look at.
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - retention;
        var deleted = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            OutboxLogging.LogRetentionApplied(logger, s_outboxName, deleted, retention);
        }
    }
}
