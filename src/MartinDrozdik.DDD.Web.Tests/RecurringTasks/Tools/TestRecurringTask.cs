using System.Threading.Channels;
using MartinDrozdik.DDD.Web.RecurringTasks;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;

/// <summary>
/// A testing <see cref="IRecurringTask"/> that records every iteration and lets a test decide how each one behaves.
/// </summary>
/// <remarks>
/// Iterations complete immediately unless a test asks for something else through:
/// <list type="bullet">
///     <item><see cref="BlockIteration(int)"/></item>
///     <item><see cref="FailIteration(int, Exception)"/></item>
///     <item><see cref="HangUntilCancelled"/></item>
/// </list>
/// Each of those replaces whatever was asked for before, and all of them must be called before the host is started.
/// </remarks>
internal sealed class TestRecurringTask : IRecurringTask
{
    private readonly Channel<int> _started = Channel.CreateUnbounded<int>();
    private Func<int, CancellationToken, Task> _behaviour = (_, _) => Task.CompletedTask;
    private TaskCompletionSource? _blockedIteration;
    private int _runCount;

    /// <summary>
    /// Gets how many iterations have started so far.
    /// </summary>
    public int RunCount => Volatile.Read(ref _runCount);

    /// <summary>
    /// Makes the given iteration hang until <see cref="ReleaseBlockedIteration"/> is called.
    /// </summary>
    /// <param name="run">The one-based iteration to block.</param>
    public void BlockIteration(int run)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _blockedIteration = gate;
        _behaviour = (current, _) => current == run ? gate.Task : Task.CompletedTask;
    }

    /// <summary>
    /// Lets the iteration blocked by <see cref="BlockIteration(int)"/> finish.
    /// </summary>
    public void ReleaseBlockedIteration()
    {
        Assert.NotNull(_blockedIteration);
        _blockedIteration.SetResult();
    }

    /// <summary>
    /// Makes the given iteration throw.
    /// </summary>
    /// <param name="run">The one-based iteration to fail.</param>
    /// <param name="exception">What it throws.</param>
    public void FailIteration(int run, Exception exception)
    {
        _behaviour = (current, _) => current == run ? Task.FromException(exception) : Task.CompletedTask;
    }

    /// <summary>
    /// Makes every iteration run until its own cancellation token stops it, as a task that ignores its timeout would.
    /// </summary>
    public void HangUntilCancelled()
    {
        _behaviour = (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var run = Interlocked.Increment(ref _runCount);

        // Recorded before the behaviour runs, so a test can observe iterations that block or throw
        await _started.Writer.WriteAsync(run, CancellationToken.None);

        await _behaviour(run, cancellationToken);
    }

    /// <summary>
    /// Waits until the next iteration starts.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the test gives up waiting.</param>
    /// <returns>The one-based number of the iteration that started.</returns>
    public async Task<int> WaitForRunAsync(CancellationToken cancellationToken)
    {
        return await _started.Reader.ReadAsync(cancellationToken);
    }
}
