using MartinDrozdik.DDD.Web.RecurringTasks;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// The built-in schedule for the outbox, added with <see cref="HostApplicationBuilderExtensions.AddOutboxDispatchRecurringTask"/>.
/// </summary>
/// <remarks>
/// A deliberately thin shell over <see cref="IOutboxProcessor"/>.
/// Applications that prefer another scheduler (Quartz.NET etc.) simply never add this task and call the processor themselves.
/// </remarks>
/// <param name="processor">The processor that does the actual delivery.</param>
public sealed class OutboxDispatchRecurringTask(IOutboxProcessor processor) : IRecurringTask
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken)
        => processor.ProcessPendingAsync(cancellationToken);
}
