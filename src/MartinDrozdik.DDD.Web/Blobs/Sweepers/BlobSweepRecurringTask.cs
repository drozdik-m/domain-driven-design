using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Web.RecurringTasks;

namespace MartinDrozdik.DDD.Web.Blobs.Sweepers;

/// <summary>
/// Runs <see cref="IBlobSweeper"/> on a schedule.
/// </summary>
/// <remarks>
/// An application can drive <see cref="IBlobSweeper"/> from its own scheduler and never register this at all.
/// </remarks>
/// <param name="sweeper">The sweeper to run.</param>
public sealed class BlobSweepRecurringTask(IBlobSweeper sweeper) : IRecurringTask
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) => sweeper.SweepAsync(cancellationToken);
}
