using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Delivers pending messages of the outbox over <typeparamref name="TDbContext"/>.
/// This is the delivery engine, deliberately independent of whatever schedules it.
/// </summary>
/// <remarks>
/// <para>
/// Applications that already run a scheduler - Quartz.NET, Hangfire, an external cron hitting an endpoint - simply create a job calling <see cref="ProcessPendingAsync"/>.
/// </para>
/// <para>
/// It only ever reads the table of <typeparamref name="TDbContext"/>, and only reads, claims and delivers the message types registered with its outbox.
/// A row of any other type is left untouched.
/// </para>
/// <para>
/// Processors in separate scopes or application instances may run at the same time: a lease and an optimistic concurrency token ensure single delivery.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class InvoiceOutboxJob(IOutboxProcessor&lt;InvoiceDbContext&gt; processor) : IJob
/// {
///     public Task Execute(IJobExecutionContext context)
///         =&gt; processor.ProcessPendingAsync(context.CancellationToken);
/// }
/// </code>
/// </example>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
#pragma warning disable S2326 // Unused type parameters - TDbContext identifies the outbox
public interface IOutboxProcessor<TDbContext>
#pragma warning restore S2326
    where TDbContext : DbContext
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
