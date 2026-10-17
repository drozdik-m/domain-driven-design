using MartinDrozdik.DDD.Extensions;
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
    /// The prefix of the name an outbox health check is reported under by default: <c>outbox-{context}</c>.
    /// </summary>
    public const string DefaultNamePrefix = "outbox";

    /// <summary>
    /// Adds a health check reporting dead-lettered messages and the backlog of the outbox of <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reported as <c>outbox-{context}</c>, e.g. <c>outbox-InvoiceDbContext</c>, and tagged <c>ready</c>.
    /// </para>
    /// <para>
    /// The check is degraded while any message is dead-lettered or more than <paramref name="degradedBacklogThreshold"/> messages are overdue,
    /// unhealthy only when the table cannot be read, and healthy otherwise.
    /// Degraded keeps the instance ready, so one poison message never takes the instances sharing the table out of load balancing.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/> to extend.</param>
    /// <param name="degradedBacklogThreshold">The overdue count above which the outbox is degraded.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddAppHealthChecks(checks =&gt; checks.AddOutboxHealthCheck&lt;InvoiceDbContext&gt;());
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddOutboxHealthCheck<TDbContext>(
        this IHealthChecksBuilder builder,
        int degradedBacklogThreshold = 1000)
        where TDbContext : DbContext
        => builder.AddOutboxHealthCheck<TDbContext>(
            $"{DefaultNamePrefix}-{typeof(TDbContext).GetReadableName()}",
            ["ready"],
            degradedBacklogThreshold);

    /// <summary>
    /// Adds a health check reporting dead-lettered messages and the backlog of the outbox of <typeparamref name="TDbContext"/>,
    /// under a name and tags of your choice.
    /// </summary>
    /// <remarks>
    /// The check is degraded while any message is dead-lettered or more than <paramref name="degradedBacklogThreshold"/> messages are overdue,
    /// unhealthy only when the table cannot be read, and healthy otherwise.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/> to extend.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="tags">Tags of the health check.</param>
    /// <param name="degradedBacklogThreshold">The overdue count above which the outbox is degraded.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddAppHealthChecks(checks =&gt; checks.AddOutboxHealthCheck&lt;InvoiceDbContext&gt;("invoice-outbox", ["ready", "outbox"]));
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddOutboxHealthCheck<TDbContext>(
        this IHealthChecksBuilder builder,
        string name,
        IEnumerable<string> tags,
        int degradedBacklogThreshold = 1000)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(degradedBacklogThreshold);

        return builder.AddTypeActivatedCheck<OutboxHealthCheck<TDbContext>>(
            name,
            failureStatus: HealthStatus.Unhealthy,
            tags: tags,
            args: [degradedBacklogThreshold]);
    }
}
