namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Delivers pending outbox messages.
/// This is the delivery engine, deliberately independent of whatever schedules it.
/// </summary>
/// <remarks>
/// <para>
/// Applications that already run a scheduler - Quartz.NET, Hangfire, an external cron hitting an endpoint - should simply create a job and invoke call <see cref="ProcessPendingAsync"/>.
/// </para>
/// <para>
/// Calling this concurrently is safe (optimistic concurrency).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OutboxJob(IOutboxProcessor processor) : IJob
/// {
///     public Task Execute(IJobExecutionContext context)
///         =&gt; processor.ProcessPendingAsync(context.CancellationToken);
/// }
/// </code>
/// </example>
public interface IOutboxProcessor
{
    /// <summary>
    /// Claims and delivers a single batch of pending messages, then applies the retention policy.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>
    /// A task with the number of messages delivered successfully in this batch.
    /// Messages that were retried, dead-lettered or claimed by another processor are not counted.
    /// </returns>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}
