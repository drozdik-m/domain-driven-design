using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MartinDrozdik.DDD.Web.Outbox.Health;

/// <summary>
/// Reports whether the outbox of <typeparamref name="TDbContext"/> is draining.
/// </summary>
/// <remarks>
/// <para>
/// Dead-lettered messages are the interesting signal: they are work the application accepted and then gave up on.
/// They and a growing backlog of overdue messages are reported as degraded.
/// The table is shared and dead letters are kept until somebody deletes them, so an unhealthy outbox would take every instance out of load balancing at once, and keep it out.
/// </para>
/// <para>
/// Unhealthy is left to a check that fails outright, e.g. a table that cannot be read.
/// </para>
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="dbContext">The context the counts are read from.</param>
/// <param name="degradedBacklogThreshold">The overdue count above which the outbox is reported as degraded.</param>
/// <param name="timeProvider">Source of the current instant.</param>
internal sealed class OutboxHealthCheck<TDbContext>(
    TDbContext dbContext,
    int degradedBacklogThreshold,
    TimeProvider timeProvider) : IHealthCheck
    where TDbContext : DbContext
{
    private static readonly string s_outboxName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // One round trip that seeks the (ProcessedAt, FailedAt, AvailableAt) index and never reads the delivered rows,
        // which pile up without retention.
        // A dead letter never has ProcessedAt set, so the filter keeps every one of them
        var counts = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DeadLettered = g.Count(m => m.FailedAt != null),
                Pending = g.Count(m => m.FailedAt == null),
                Overdue = g.Count(m => m.FailedAt == null && m.AvailableAt <= now),
            })
            .SingleOrDefaultAsync(cancellationToken);

        // No undelivered row means no group at all
        var deadLettered = counts?.DeadLettered ?? 0;
        var pending = counts?.Pending ?? 0;
        var overdue = counts?.Overdue ?? 0;

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["deadLettered"] = deadLettered,
            ["pending"] = pending,
            ["overdue"] = overdue,
        };

        var problems = new List<string>(2);

        if (deadLettered > 0)
        {
            problems.Add($"{deadLettered} dead-lettered message(s) that will never be delivered");
        }

        if (overdue > degradedBacklogThreshold)
        {
            problems.Add($"{overdue} overdue message(s), above the threshold of {degradedBacklogThreshold}");
        }

        if (problems.Count > 0)
        {
            return HealthCheckResult.Degraded($"The outbox of {s_outboxName} holds {string.Join(" and ", problems)}.", data: data);
        }

        return HealthCheckResult.Healthy($"The outbox of {s_outboxName} holds {pending} pending message(s), {overdue} of them overdue.", data);
    }
}
