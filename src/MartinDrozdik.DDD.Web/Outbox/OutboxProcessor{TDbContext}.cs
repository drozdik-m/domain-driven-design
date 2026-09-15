using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Dispatch;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
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
/// <param name="scopeFactory">Creates the scope each handler is resolved from.</param>
/// <param name="options">Options driving batching, leases, retries and retention.</param>
/// <param name="timeProvider">Source of the current instant.</param>
/// <param name="logger">Logger for delivery outcomes.</param>
internal sealed class OutboxProcessor<TDbContext>(
    TDbContext context,
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TDbContext>> logger) : IOutboxProcessor
    where TDbContext : DbContext
{
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

            if (!await TryClaimAsync(message, claimId, cancellationToken))
            {
                continue;
            }

            claimed++;
            if (await TryDeliverAsync(message, cancellationToken))
            {
                dispatched++;
            }
        }

        if (claimed > 0)
        {
            OutboxLogging.LogBatchCompleted(logger, dispatched, claimed);
        }

        await ApplyRetentionAsync(cancellationToken);

        return dispatched;
    }

    /// <summary>
    /// Reads the messages that may be delivered right now.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The messages to attempt, oldest first.</returns>
    private async Task<IReadOnlyList<OutboxMessage>> GetDeliverableAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null &&
                m.FailedAt == null &&
                m.AvailableAt <= now &&
                (m.ClaimedUntil == null || m.ClaimedUntil < now))
            .OrderBy(m => m.Id) // GUID v7 sorts by creation time, so this is oldest first
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
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
    /// <param name="message">The claimed message.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>True when the message was delivered, else false.</returns>
    private async Task<bool> TryDeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetKeyedService<IOutboxDispatcher>(message.MessageType.Key);

        if (dispatcher is null)
        {
            // Not necessarily permanent - during a rolling deployment an instance may see new/old messages.
            // Let them be retried...
            OutboxLogging.LogUnknownMessageType(logger, message.Id, message.MessageType.Key);
            await RecordFailureAsync(
                message,
                new OutboxException($"No handler is registered for outbox message type '{message.MessageType}'. Register one with WithMessage<...>() when calling AddOutbox, or delete the stored rows."),
                cancellationToken);

            return false;
        }

        try
        {
            await dispatcher.DispatchAsync(message.Payload, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down... The lease is left in place and expires on its own.
            throw;
        }
#pragma warning disable CA1031 // Do not catch general exception types - any handler failure is a retryable delivery failure
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await RecordFailureAsync(message, exception, cancellationToken);
            return false;
        }

        message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
        if (!await TrySaveAsync(message, cancellationToken))
        {
            return false;
        }

        OutboxLogging.LogDispatched(logger, message.Id, message.MessageType.Key, message.Attempts);
        return true;
    }

    /// <summary>
    /// Schedules the next attempt, or dead-letters the message when the retry delays run out.
    /// </summary>
    /// <param name="message">The message that failed.</param>
    /// <param name="exception">The failure that prevented delivery.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the outcome has been stored.</returns>
    private Task<bool> RecordFailureAsync(OutboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var retryDelays = options.Value.RetryDelays;
        var attemptIndex = message.Attempts;

        if (attemptIndex < retryDelays.Count)
        {
            var delay = retryDelays[attemptIndex];
            message.MarkRetrying(now, delay, exception.ToString());
            OutboxLogging.LogRetrying(logger, exception, message.Id, message.MessageType.Key, message.Attempts, delay);
        }
        else
        {
            message.MarkFailed(now, exception.ToString());
            OutboxLogging.LogDeadLettered(logger, exception, message.Id, message.MessageType.Key, message.Attempts);
        }

        return TrySaveAsync(message, cancellationToken);
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
            OutboxLogging.LogClaimLost(logger, message.Id);
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
            OutboxLogging.LogRetentionApplied(logger, deleted, retention);
        }
    }
}
