using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// Deletes old delivered messages held in <typeparamref name="TDbContext"/>, chunk by chunk.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="context">The context the messages are deleted from.</param>
/// <param name="options">How long delivered messages are kept, and how many go per statement.</param>
/// <param name="timeProvider">Source of the current instant.</param>
/// <param name="logger">Logger for the deletes.</param>
internal sealed class OutboxRetention<TDbContext>(
    TDbContext context,
    IOptions<OutboxRetentionOptions<TDbContext>> options,
    TimeProvider timeProvider,
    ILogger<OutboxRetention<TDbContext>> logger) : IOutboxRetention<TDbContext>
    where TDbContext : DbContext
{
    private static readonly string s_outboxName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public async Task<int> ApplyAsync(CancellationToken cancellationToken)
    {
        var retention = options.Value.Retention;
        var batchSize = options.Value.BatchSize;

        // Dead-lettered messages are deliberately kept - someone has to look at them and decide what to do
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - retention;
        var deleted = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            // Get the ids first
            // MySQL rejects a LIMIT in a subquery of the table being deleted from
            var chunk = await context.Set<OutboxMessage>()
                .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff)
                .OrderBy(m => m.ProcessedAt)
                .Take(batchSize)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (chunk.Count == 0)
            {
                break;
            }

            // The locks go before the next chunk, so inserts are never blocked for long
            deleted += await context.Set<OutboxMessage>()
                .Where(m => chunk.Contains(m.Id))
                .ExecuteDeleteAsync(cancellationToken);

            if (chunk.Count < batchSize)
            {
                break;
            }
        }

        if (deleted > 0)
        {
            OutboxLogging.LogRetentionApplied(logger, s_outboxName, deleted, retention);
        }

        return deleted;
    }
}
