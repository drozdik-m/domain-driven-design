using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Extensions for <see cref="IOutboxProcessor{TDbContext}"/>.
/// </summary>
public static class OutboxProcessorExtensions
{
    /// <summary>
    /// How many batches in a row may find messages and reserve none of them before <see cref="DrainAsync{TDbContext}"/> stops.
    /// </summary>
    internal const int MaxBatchesWithoutReservation = 3;

    /// <summary>
    /// Processes batch after batch while <see cref="OutboxBatchResult.MayHaveMore"/>, to drain a backlog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Also stops after three batches in a row that found messages and reserved none of them.
    /// Usually another processor reserved them first, and it delivers them.
    /// A row this processor can read but never reserve would otherwise make it repeat the same two queries without a pause.
    /// </para>
    /// <para>
    /// Cancellation stops it between batches.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <param name="processor">The processor to call.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A task with the total number of messages delivered successfully.</returns>
    public static async Task<int> DrainAsync<TDbContext>(this IOutboxProcessor<TDbContext> processor, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(processor);

        var dispatched = 0;
        var batchesWithoutReservation = 0;
        OutboxBatchResult result;
        do
        {
            result = await processor.ProcessPendingAsync(cancellationToken);
            dispatched += result.Dispatched;
            batchesWithoutReservation = result.Reserved == 0 ? batchesWithoutReservation + 1 : 0;
        }
        while (result.MayHaveMore &&
               batchesWithoutReservation < MaxBatchesWithoutReservation &&
               !cancellationToken.IsCancellationRequested);

        return dispatched;
    }
}
