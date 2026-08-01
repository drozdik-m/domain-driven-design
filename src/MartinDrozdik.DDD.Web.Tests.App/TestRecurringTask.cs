using System.Threading.Channels;
using MartinDrozdik.DDD.Web.RecurringTasks;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// A testing recurring task that records its runs in a singleton <see cref="TestRecurringTaskRuns"/>.
/// </summary>
/// <param name="runs">Records the runs for the test to observe.</param>
public sealed class TestRecurringTask(TestRecurringTaskRuns runs) : IRecurringTask
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken)
    {
        runs.Record();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Singleton recording every run of <see cref="TestRecurringTask"/>.
/// </summary>
public sealed class TestRecurringTaskRuns
{
    private readonly Channel<int> _runs = Channel.CreateUnbounded<int>();
    private int _count;

    /// <summary>
    /// Gets how many times the task has run.
    /// </summary>
    public int Count => Volatile.Read(ref _count);

    /// <summary>
    /// Records a run.
    /// </summary>
    public void Record()
    {
        _runs.Writer.TryWrite(Interlocked.Increment(ref _count));
    }

    /// <summary>
    /// Waits until the next run happens.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the test gives up waiting.</param>
    /// <returns>The one-based number of the run.</returns>
    public async Task<int> WaitForRunAsync(CancellationToken cancellationToken)
    {
        return await _runs.Reader.ReadAsync(cancellationToken);
    }
}
