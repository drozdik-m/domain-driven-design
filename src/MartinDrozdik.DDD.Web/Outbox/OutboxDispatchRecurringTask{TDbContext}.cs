using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// The built-in schedule for the outbox over <typeparamref name="TDbContext"/>,
/// added with <see cref="HostApplicationBuilderExtensions.AddOutboxDispatchRecurringTask{TDbContext}"/>.
/// </summary>
/// <remarks>
/// A deliberately thin shell over <see cref="IOutboxProcessor{TDbContext}"/>.
/// One iteration processes batch after batch while <see cref="OutboxBatchResult.MayHaveMore"/>, so it ends once a batch finds nothing to deliver.
/// Cancellation, e.g. by the timeout of the iteration, stops it between batches.
/// Applications that prefer another scheduler (Quartz.NET etc.) simply never add this task and call the processor themselves.
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
/// <param name="processor">The processor that does the actual delivery.</param>
public sealed class OutboxDispatchRecurringTask<TDbContext>(IOutboxProcessor<TDbContext> processor) : IRecurringTask
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        OutboxBatchResult result;
        do
        {
            result = await processor.ProcessPendingAsync(cancellationToken);
        }
        while (result.MayHaveMore && !cancellationToken.IsCancellationRequested);
    }
}
