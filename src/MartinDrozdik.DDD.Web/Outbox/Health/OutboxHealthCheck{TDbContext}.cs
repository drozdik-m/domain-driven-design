using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MartinDrozdik.DDD.Web.Outbox.Health;

/// <summary>
/// Reports whether the outbox of <typeparamref name="TDbContext"/> is draining.
/// </summary>
/// <remarks>
/// Dead-lettered messages are the interesting signal: they are work the application accepted and then gave up on.
/// A growing backlog of overdue messages is reported as degraded rather than unhealthy.
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
        var messages = dbContext.Set<OutboxMessage>();

        var deadLettered = await messages.CountAsync(m => m.FailedAt != null, cancellationToken);
        var pending = await messages.CountAsync(m => m.ProcessedAt == null && m.FailedAt == null, cancellationToken);
        var overdue = await messages.CountAsync(m => m.ProcessedAt == null && m.FailedAt == null && m.AvailableAt <= now, cancellationToken);

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["deadLettered"] = deadLettered,
            ["pending"] = pending,
            ["overdue"] = overdue,
        };

        if (deadLettered > 0)
        {
            return HealthCheckResult.Unhealthy($"The outbox of {s_outboxName} holds {deadLettered} dead-lettered message(s) that will never be delivered.", data: data);
        }

        if (overdue > degradedBacklogThreshold)
        {
            return HealthCheckResult.Degraded($"The outbox of {s_outboxName} has {overdue} overdue message(s), above the threshold of {degradedBacklogThreshold}.", data: data);
        }

        return HealthCheckResult.Healthy($"The outbox of {s_outboxName} holds {pending} pending message(s), {overdue} of them overdue.", data);
    }
}
