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
/// Processors in separate scopes or application instances may run at the same time.
/// Each reserves a batch of its own up front, and a lease plus an optimistic concurrency token on every message ensure single delivery.
/// </para>
/// <para>
/// The processor takes the <typeparamref name="TDbContext"/> of its scope for itself: resolve it from a scope of its own, not one that also serves other work.
/// Every batch clears the change tracker of that context, so pending changes of other code in the scope would be dropped or committed with a claim.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class InvoiceOutboxJob(IOutboxProcessor&lt;InvoiceDbContext&gt; processor) : IJob
/// {
///     public async Task Execute(IJobExecutionContext context)
///     {
///         // Keep going until a batch finds nothing, to drain a backlog
///         OutboxBatchResult result;
///         do
///         {
///             result = await processor.ProcessPendingAsync(context.CancellationToken);
///         }
///         while (result.MayHaveMore &amp;&amp; !context.CancellationToken.IsCancellationRequested);
///     }
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
    /// Reserves and delivers a single batch of pending messages.
    /// </summary>
    /// <remarks>
    /// A run cancelled between two messages stops there and returns what it delivered so far.
    /// The messages it reserved but did not start are released for other processors.
    /// Clears the change tracker of <typeparamref name="TDbContext"/>.
    /// </remarks>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A task with the outcome of the batch.</returns>
    Task<OutboxBatchResult> ProcessPendingAsync(CancellationToken cancellationToken);
}
