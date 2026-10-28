using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// The built-in schedule of the retention of the outbox over <typeparamref name="TDbContext"/>,
/// added with <see cref="HostApplicationBuilderExtensions.AddOutboxRetentionRecurringTask{TDbContext}"/>.
/// </summary>
/// <remarks>
/// A thin shell over <see cref="IOutboxRetention{TDbContext}.ApplyAsync"/>.
/// Applications that prefer another scheduler (Quartz.NET etc.) simply never add this task and call the retention themselves.
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
/// <param name="retention">The retention that deletes the old messages.</param>
public sealed class OutboxRetentionRecurringTask<TDbContext>(IOutboxRetention<TDbContext> retention) : IRecurringTask
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) => retention.ApplyAsync(cancellationToken);
}
