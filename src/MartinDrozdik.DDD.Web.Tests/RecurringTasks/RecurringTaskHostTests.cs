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
    public async Task Triggers_raised_during_an_iteration_are_coalesced_into_a_single_extra_run()
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
        await harness.Task.WaitForRunAsync(TestContext.Current.CancellationToken);

        // Assert
        // Timer 2 waits after the first iteration, timer 3 after the single coalesced one.
        // Reaching timer 3 without any further run proves the fifty requests collapsed into one.
        await harness.Time.WaitForTimerAsync(3);
        Assert.Equal(2, harness.Task.RunCount);
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
}
