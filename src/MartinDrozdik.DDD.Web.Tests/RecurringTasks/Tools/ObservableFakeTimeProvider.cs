using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;

/// <summary>
/// A <see cref="FakeTimeProvider"/> that lets a test wait until the background code under test has actually created its timer.
/// </summary>
/// <remarks>
/// <para>
/// <b>The race this removes.</b>
/// <see cref="RecurringTaskHost{TTask}"/> runs on its own task, so a returning <c>StartAsync</c> says nothing about how far the loop has got.
/// <see cref="FakeTimeProvider.Advance(TimeSpan)"/> only fires timers that exist at that instant, so advancing before the loop reached its wait does nothing at all —
/// and the loop then arms its timer against the already-advanced clock, waiting out a delay the test has no reason to advance a second time.
/// The test hangs until its cancellation token gives up, and only on machines slow enough to lose the race.
/// </para>
/// <para>
/// <b>What the count counts.</b>
/// Every wait in the host goes through <c>new CancellationTokenSource(delay, timeProvider)</c>,
/// which calls <see cref="TimeProvider.CreateTimer(TimerCallback, object, TimeSpan, TimeSpan)"/> exactly once.
/// Counting those calls is therefore a precise signal for <i>the loop has arrived at a wait point</i>, which is what <see cref="WaitForTimerAsync(int)"/> blocks on.
/// Timers are numbered cumulatively in creation order: timer 1 is the initial delay, timer 2 the gap after the first iteration, and so on.
/// A configured per-iteration timeout arms a timer of its own, so it takes a number too.
/// </para>
/// <para>
/// <b>Why the wait cannot itself go wrong.</b>
/// Advancing a moment too early is harmless: the timer is armed inside the <see cref="CancellationTokenSource"/> constructor, just before the loop parks on its <c>await</c>,
/// and a token already cancelled by then simply throws straight away.
/// Arming the wait too late is harmless as well, because the count only ever grows and is never consumed.
/// Only advancing too early can hang, and that is exactly the case this class rules out.
/// </para>
/// <para>
/// It doubles as an assertion. Reaching timer <c>n</c> proves iteration <c>n - 1</c> finished and the loop is parked again,
/// so a test can assert that nothing <i>further</i> happened without the "it may simply not have happened yet" hole that a real-time <c>Task.Delay</c> would leave open.
/// </para>
/// </remarks>
internal sealed class ObservableFakeTimeProvider : FakeTimeProvider
{
    private readonly Lock _lock = new();
    private readonly List<(int Count, TaskCompletionSource Source)> _waiters = [];
    private int _timersCreated;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);

        List<TaskCompletionSource> released;
        lock (_lock)
        {
            _timersCreated++;
            released = _waiters.FindAll(waiter => waiter.Count <= _timersCreated).ConvertAll(waiter => waiter.Source);
            _waiters.RemoveAll(waiter => waiter.Count <= _timersCreated);
        }

        // Completed outside the lock so continuations never run while it is held
        foreach (var source in released)
        {
            source.TrySetResult();
        }

        return timer;
    }

    /// <summary>
    /// Waits until at least <paramref name="count"/> timers have been created against this provider.
    /// </summary>
    /// <remarks>
    /// The count is cumulative and only ever grows, so arming a wait after the timer was already created returns a completed task rather than hanging — there is no wakeup to miss.
    /// </remarks>
    /// <param name="count">The number of timers to wait for, counted from the start of the test.</param>
    /// <returns>A <see cref="Task"/> that completes once that many timers exist.</returns>
    public Task WaitForTimerAsync(int count)
    {
        lock (_lock)
        {
            if (_timersCreated >= count)
            {
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, source));
            return source.Task;
        }
    }
}
