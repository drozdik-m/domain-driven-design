using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// Deletes old delivered messages of the outbox over <typeparamref name="TDbContext"/>.
/// Added with <see cref="HostApplicationBuilderExtensions.AddOutboxRetention{TDbContext}"/>.
/// </summary>
/// <remarks>
/// Schedule it with <see cref="HostApplicationBuilderExtensions.AddOutboxRetentionRecurringTask{TDbContext}"/>,
/// or call it from your own scheduler, far less often than the delivery, e.g. hourly or nightly.
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
#pragma warning disable S2326 // Unused type parameters - outbox TDbContext
public interface IOutboxRetention<TDbContext>
#pragma warning restore S2326
    where TDbContext : DbContext
{
    /// <summary>
    /// Deletes delivered messages older than <see cref="OutboxRetentionOptions{TDbContext}.Retention"/>.
    /// </summary>
    /// <remarks>
    /// Deletes in chunks of <see cref="OutboxRetentionOptions{TDbContext}.BatchSize"/>, until no old message is left.
    /// Dead-lettered messages are never deleted.
    /// </remarks>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A task with the number of messages deleted.</returns>
    Task<int> ApplyAsync(CancellationToken cancellationToken);
}
