using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MartinDrozdik.DDD.Web.Outbox.Health;

/// <summary>
/// Extensions for <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// The name the outbox health check is reported under by default.
    /// </summary>
    public const string DefaultName = "outbox";

    /// <summary>
    /// Adds a health check reporting dead-lettered messages and the size of the outbox backlog.
    /// </summary>
    /// <remarks>
    /// The check is unhealthy while any message is dead-lettered, degraded while the backlog exceeds
    /// <paramref name="degradedBacklogThreshold"/>, and healthy otherwise. Counts are always reported
    /// in the health check data, so a probe response shows the backlog even when it is healthy.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/> to extend.</param>
    /// <param name="degradedBacklogThreshold">The pending count above which the outbox is degraded.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="tags">Tags of the health check, "ready" by default.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddAppHealthChecks(checks =&gt; checks.AddOutboxHealthCheck&lt;InvoiceDbContext&gt;());
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddOutboxHealthCheck<TDbContext>(
        this IHealthChecksBuilder builder,
        int degradedBacklogThreshold = 1000,
        string name = DefaultName,
        IEnumerable<string>? tags = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(degradedBacklogThreshold);

        return builder.AddTypeActivatedCheck<OutboxHealthCheck<TDbContext>>(
            name,
            failureStatus: HealthStatus.Unhealthy,
            tags: tags ?? ["ready"],
            args: [degradedBacklogThreshold]);
    }
}
