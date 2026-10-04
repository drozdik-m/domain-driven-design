using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies the scheduling, triggering and failure behaviour of <see cref="RecurringTaskHost{TTask}"/>.
/// </summary>
/// <remarks>
/// Every delay is driven by a fake clock.
/// The <see cref="ObservableFakeTimeProvider.WaitForTimerAsync(int)"/> calls are what keeps that deterministic — see <see cref="ObservableFakeTimeProvider"/>.
/// </remarks>
public class RecurringTaskHostTests
{
    /// <summary>
    /// What the host logs when an iteration starts because of <see cref="IRecurringTaskTrigger{TTask}.Trigger"/>.
    /// </summary>
    private const string TriggeredOnDemand = "Triggered on demand: True";

    private static readonly TimeSpan s_initialDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_period = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Task_does_not_run_before_the_initial_delay_elapses()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        harness.Time.Advance(s_initialDelay - TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(0, harness.Task.RunCount);
    }

    [Fact]
    public async Task Task_runs_after_the_initial_delay_elapses()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        harness.Time.Advance(s_initialDelay);

        // Assert
        var run = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, run);
    }

    [Fact]
    public async Task Task_runs_again_every_period()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.Time.WaitForTimerAsync(2);
        harness.Time.Advance(s_period);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        await harness.Time.WaitForTimerAsync(3);
        harness.Time.Advance(s_period);
        var thirdRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, thirdRun);
    }

    [Fact]
    public async Task Period_is_measured_after_the_iteration_completes()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.BlockIteration(1);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        // The first iteration is still running, so the period has not even started counting yet
        harness.Time.Advance(s_period * 10);

        // Assert
        Assert.Equal(1, harness.Task.RunCount);

        // Only once the iteration finishes does the gap begin
        harness.Task.ReleaseBlockedIteration();
        await harness.Time.WaitForTimerAsync(2);
        harness.Time.Advance(s_period);

        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, secondRun);
    }

    [Fact]
    public async Task Trigger_runs_the_task_immediately_without_waiting_for_the_period()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Act
        harness.Trigger.Trigger();

        // Assert
        // No time is advanced at all - the run happens purely because of the trigger
        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, secondRun);
    }

    [Fact]
    public async Task Trigger_raised_during_the_initial_delay_starts_the_first_iteration_immediately()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        harness.Trigger.Trigger();

        // Assert
        var run = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, run);
    }

    [Fact]
    public async Task Triggers_raised_during_an_iteration_start_another_run_right_after_it()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.BlockIteration(1);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        for (var i = 0; i < 50; i++)
        {
            harness.Trigger.Trigger();
        }

        harness.Task.ReleaseBlockedIteration();
        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        // No time was advanced, so only a pending request could have started it. That the fifty requests collapse into one
        // is asserted on the trigger itself in RecurringTaskTriggerTests: reaching the next timer here would not prove the loop is idle.
        Assert.Equal(2, secondRun);
        Assert.Contains(TriggeredOnDemand, IterationStarts(harness)[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failing_iteration_is_logged_as_an_error_and_the_loop_keeps_running()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.FailIteration(1, new InvalidOperationException("Boom"));

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.Time.WaitForTimerAsync(2);
        harness.Time.Advance(s_period);
        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, secondRun);
        Assert.Contains(
            harness.Logger.At(LogLevel.Error),
            entry => entry.Exception is InvalidOperationException
                && entry.Message.Contains(nameof(TestRecurringTask), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Iteration_exceeding_the_configured_timeout_is_cancelled()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: timeout));
        harness.Task.HangUntilCancelled();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        // The iteration is hanging; timer 2 is the timeout the host armed around it
        await harness.Time.WaitForTimerAsync(2);
        harness.Time.Advance(timeout);

        // Assert
        // The loop survives the timeout and schedules the next iteration
        await harness.Time.WaitForTimerAsync(3);
        Assert.Contains(
            harness.Logger.At(LogLevel.Warning),
            entry => entry.Message.Contains(nameof(TestRecurringTask), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Disabled_task_never_runs()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule(enabled: false));

        // Act
        await harness.StartAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(harness.ExecuteTask);
        await harness.ExecuteTask;
        harness.Trigger.Trigger();

        // Assert
        Assert.Equal(0, harness.Task.RunCount);
        Assert.Contains(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("is disabled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Host_stops_promptly_when_the_stopping_token_is_cancelled()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        // No time is advanced - shutdown must not have to wait out the remaining delay
        await harness.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, harness.Task.RunCount);
        Assert.Contains(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("is stopping", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Task_does_not_run_even_a_moment_before_the_initial_delay_elapses()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        // The trigger is a fence. Asserting "no run yet" straight after Advance proves nothing, because an iteration started
        // by the timer begins on another thread a moment later. If the timer fired early, the first run is the timer's and the
        // trigger is left over; if it did not, the first run can only be the trigger's.
        harness.Time.Advance(s_initialDelay - TimeSpan.FromMilliseconds(1));
        harness.Trigger.Trigger();
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(TriggeredOnDemand, IterationStarts(harness)[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Task_does_not_run_again_even_a_moment_before_the_period_elapses()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Act
        // The same fence as above: a second run started by the period timer would leave the trigger unconsumed
        harness.Time.Advance(s_period - TimeSpan.FromMilliseconds(1));
        harness.Trigger.Trigger();
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(TriggeredOnDemand, IterationStarts(harness)[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Trigger_raised_before_the_first_iteration_is_served_by_it_rather_than_by_a_second_run()
    {
        // Arrange
        // No initial delay - the default - so the first iteration starts without ever waiting on the trigger
        using var harness = new RecurringTaskTestHarness(new RecurringTaskOptions<TestRecurringTask>
        {
            InitialDelay = TimeSpan.Zero,
            Period = s_period,
        });
        harness.Task.BlockIteration(1);
        harness.Trigger.Trigger();

        // Act
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        // The request predates the first iteration, so that iteration already does what was asked for. A request still
        // pending while it runs is one the loop serves again as soon as it ends - a second, redundant run.
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await harness.Trigger.WaitAsync(timeout.Token));
        harness.Task.ReleaseBlockedIteration();
    }

    [Fact]
    public async Task Timeout_is_reported_even_when_the_task_honours_it_by_returning()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: timeout));
        harness.Task.ReturnWhenCancelled();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Act
        // Stopping early on cancellation is exactly what IRecurringTask asks of a task, yet it must not hide the overrun
        harness.Time.Advance(timeout);
        await harness.Time.WaitForTimerAsync(3);

        // Assert
        Assert.Contains(
            harness.Logger.At(LogLevel.Warning),
            entry => entry.Message.Contains(nameof(TestRecurringTask), StringComparison.Ordinal)
                && entry.Message.Contains("timeout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Task_ignoring_its_timeout_is_waited_for_rather_than_overlapped()
    {
        // Arrange
        // The blocked iteration ignores its token entirely, unlike HangUntilCancelled
        var timeout = TimeSpan.FromSeconds(5);
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: timeout));
        harness.Task.BlockIteration(1);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Act
        harness.Time.Advance(timeout + (s_period * 10));
        harness.Trigger.Trigger();

        // Assert
        // The timeout only cancels the token. The loop does not move on to the next wait until the iteration really ends,
        // so iterations never overlap. Timer 3 would be that next wait.
        var nextWait = harness.Time.WaitForTimerAsync(3);
        var finished = await Task.WhenAny(nextWait, Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));
        Assert.NotSame(nextWait, finished);
        Assert.Equal(1, harness.Task.RunCount);

        // The overrun is reported while the iteration is still stuck, not only once it ends
        Assert.Contains(
            harness.Logger.At(LogLevel.Warning),
            entry => entry.Message.Contains(nameof(TestRecurringTask), StringComparison.Ordinal)
                && entry.Message.Contains("timeout", StringComparison.Ordinal));

        // Once it does end, the request raised meanwhile is honoured
        harness.Task.ReleaseBlockedIteration();
        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, secondRun);
    }

    [Fact]
    public async Task Task_returning_after_its_timeout_is_not_reported_as_finished()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: timeout));
        harness.Task.ReturnWhenCancelled();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Act
        harness.Time.Advance(timeout);
        await harness.Time.WaitForTimerAsync(3);

        // Assert
        // Exactly one warning for the overrun, and the end of the iteration says it was cut short rather than finished
        Assert.Single(harness.Logger.AtLeast(LogLevel.Warning));
        Assert.DoesNotContain(
            harness.Logger.Entries,
            entry => entry.Message.Contains("finished an iteration", StringComparison.Ordinal));
        Assert.Contains(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("cancelled by its timeout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Successful_iteration_is_logged_at_debug_under_the_category_of_its_task()
    {
        // Arrange
        // A dispatch every 30 seconds must not flood Information, and must be quietened without silencing other tasks
        using var harness = new RecurringTaskTestHarness(Schedule());
        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);

        // Act
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(2);

        // Assert
        Assert.DoesNotContain(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("finished an iteration", StringComparison.Ordinal));
        Assert.Contains(
            harness.Logger.At(LogLevel.Debug),
            entry => entry.Message.Contains("finished an iteration", StringComparison.Ordinal));
        Assert.All(
            harness.Logger.Entries,
            entry => Assert.Equal(
                "MartinDrozdik.DDD.Web.RecurringTasks.RecurringTaskHost.MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools.TestRecurringTask",
                entry.Category));
    }

    [Fact]
    public async Task Iteration_finishing_within_its_timeout_is_never_reported_as_timed_out()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: timeout));

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        // Timer 2 was the timeout of the finished iteration, timer 3 is the period wait after it.
        // Advancing past the old timeout must not wake anything left behind by it.
        await harness.Time.WaitForTimerAsync(3);
        harness.Time.Advance(timeout);

        // Assert
        Assert.Empty(harness.Logger.AtLeast(LogLevel.Warning));
        Assert.Contains(
            harness.Logger.At(LogLevel.Debug),
            entry => entry.Message.Contains("finished an iteration", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Stopping_during_an_iteration_with_a_timeout_is_not_reported_as_timed_out()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule(timeout: TimeSpan.FromSeconds(5)));
        harness.Task.ReturnWhenCancelled();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(harness.Logger.AtLeast(LogLevel.Warning));
        Assert.DoesNotContain(
            harness.Logger.Entries,
            entry => entry.Message.Contains("timeout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Stopping_during_an_iteration_cancels_its_token_and_ends_the_loop_quietly()
    {
        // Arrange
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.HangUntilCancelled();

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        // A shutdown is neither a failure nor a timeout
        Assert.NotNull(harness.ExecuteTask);
        Assert.True(harness.ExecuteTask.IsCompletedSuccessfully);
        Assert.Empty(harness.Logger.AtLeast(LogLevel.Warning));
        Assert.Contains(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("is stopping", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Stopping_during_an_iteration_that_throws_its_own_exception_is_not_reported_as_a_failure()
    {
        // Arrange
        // E.g. SqlClient reporting a cancelled command as a SqlException rather than an OperationCanceledException
        var exception = new InvalidOperationException("Operation cancelled by user.");
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.ThrowWhenCancelled(exception);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(harness.ExecuteTask);
        Assert.True(harness.ExecuteTask.IsCompletedSuccessfully);
        Assert.Empty(harness.Logger.AtLeast(LogLevel.Error));
        Assert.DoesNotContain(
            harness.Logger.Entries,
            entry => entry.Message.Contains("will run again", StringComparison.Ordinal));

        // Still recorded, in case the exception was a real failure that just happened to coincide with the shutdown
        Assert.Contains(harness.Logger.At(LogLevel.Warning), entry => entry.Exception == exception);
        Assert.Contains(
            harness.Logger.At(LogLevel.Information),
            entry => entry.Message.Contains("is stopping", StringComparison.Ordinal));
        Assert.Equal(1, harness.Task.RunCount);
    }

    [Fact]
    public async Task Task_cancelling_on_its_own_is_logged_as_a_failure_and_the_loop_keeps_running()
    {
        // Arrange
        // Neither the application stopping nor a timeout, e.g. an HTTP client giving up on its own
        using var harness = new RecurringTaskTestHarness(Schedule());
        harness.Task.FailIteration(1, new OperationCanceledException("The upstream call timed out."));

        await harness.StartAsync(TestContext.Current.CancellationToken);
        await harness.Time.WaitForTimerAsync(1);
        harness.Time.Advance(s_initialDelay);
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Act
        await harness.Time.WaitForTimerAsync(2);
        harness.Time.Advance(s_period);
        var secondRun = await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, secondRun);
        Assert.Contains(harness.Logger.At(LogLevel.Error), entry => entry.Exception is OperationCanceledException);
    }

    private static RecurringTaskOptions<TestRecurringTask> Schedule(bool enabled = true, TimeSpan? timeout = null)
    {
        return new RecurringTaskOptions<TestRecurringTask>
        {
            Enabled = enabled,
            InitialDelay = s_initialDelay,
            Period = s_period,
            Timeout = timeout,
        };
    }

    /// <summary>
    /// Reads the "starting an iteration" messages the host logged, in order, one per iteration.
    /// </summary>
    /// <param name="harness">The harness whose log is read.</param>
    /// <returns>The messages, the first iteration first.</returns>
    private static List<string> IterationStarts(RecurringTaskTestHarness harness)
    {
        return harness.Logger.Entries
            .Where(entry => entry.Message.Contains("is starting an iteration", StringComparison.Ordinal))
            .Select(entry => entry.Message)
            .ToList();
    }
}
