using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// The built-in schedule for the outbox over <typeparamref name="TDbContext"/>,
/// added with <see cref="HostApplicationBuilderExtensions.AddOutboxDispatchRecurringTask{TDbContext}"/>.
/// </summary>
/// <remarks>
/// A deliberately thin shell over <see cref="IOutboxProcessor{TDbContext}"/>.
/// Applications that prefer another scheduler (Quartz.NET etc.) simply never add this task and call the processor themselves.
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
/// <param name="processor">The processor that does the actual delivery.</param>
public sealed class OutboxDispatchRecurringTask<TDbContext>(IOutboxProcessor<TDbContext> processor) : IRecurringTask
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken)
        => processor.ProcessPendingAsync(cancellationToken);
}
