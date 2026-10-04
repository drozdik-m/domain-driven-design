using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies when <see cref="RecurringTaskRunGate"/> lets an iteration start, and how long it makes the loop wait when it does not.
/// </summary>
public class RecurringTaskRunGateTests
{
    private static readonly RecurringTaskTimeWindow s_nightly = new(new TimeOnly(22, 0), new TimeOnly(4, 0));
    private static readonly RecurringTaskTimeWindow s_daytime = new(new TimeOnly(8, 0), new TimeOnly(17, 0));

    [Fact]
    public void Gate_without_rules_always_lets_the_task_run()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(null, null);
        var now = At(1, 3, 0);

        // Act
        for (var run = 0; run < 100; run++)
        {
            gate.RecordRun(now);
        }

        var wait = gate.GetWaitBeforeRun(now);

        // Assert
        Assert.Null(wait);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(12, 30)]
    [InlineData(16, 59)]
    public void Time_inside_the_window_lets_the_task_run(int hour, int minute)
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_daytime, null);

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, hour, minute));

        // Assert
        Assert.Null(wait);
    }

    [Fact]
    public void Time_before_the_window_waits_until_it_opens_later_today()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_daytime, null);

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, 6, 30));

        // Assert
        Assert.Equal(new TimeSpan(1, 30, 0), wait);
    }

    [Fact]
    public void End_of_the_window_is_exclusive_and_waits_until_it_opens_tomorrow()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_daytime, null);

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, 17, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(15), wait);
    }

    [Theory]
    [InlineData(22, 0)]
    [InlineData(23, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 59)]
    public void Time_inside_a_window_wrapping_past_midnight_lets_the_task_run(int hour, int minute)
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_nightly, null);

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, hour, minute));

        // Assert
        Assert.Null(wait);
    }

    [Fact]
    public void Time_outside_a_window_wrapping_past_midnight_waits_until_it_opens()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_nightly, null);

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, 5, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(17), wait);
    }

    [Fact]
    public void Daily_limit_blocks_once_reached_and_waits_until_midnight()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(null, 2);
        gate.RecordRun(At(1, 9, 0));
        var afterFirstRun = gate.GetWaitBeforeRun(At(1, 10, 0));

        // Act
        gate.RecordRun(At(1, 10, 0));
        var afterSecondRun = gate.GetWaitBeforeRun(At(1, 18, 0));

        // Assert
        Assert.Null(afterFirstRun);
        Assert.Equal(TimeSpan.FromHours(6), afterSecondRun);
    }

    [Fact]
    public void Daily_limit_starts_over_on_a_new_day()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(null, 1);
        gate.RecordRun(At(1, 23, 0));

        // Act
        var wait = gate.GetWaitBeforeRun(At(2, 0, 0));

        // Assert
        Assert.Null(wait);
    }

    [Fact]
    public void Daily_limit_with_a_window_waits_until_the_window_opens_again()
    {
        // Arrange
        var gate = new RecurringTaskRunGate(s_daytime, 1);
        gate.RecordRun(At(1, 8, 0));

        // Act
        var wait = gate.GetWaitBeforeRun(At(1, 9, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(23), wait);
    }

    [Fact]
    public void Night_crossing_midnight_counts_as_one_day()
    {
        // Arrange
        // Were the day to start at midnight, a once-a-night task would run again at 00:00
        var gate = new RecurringTaskRunGate(s_nightly, 1);
        gate.RecordRun(At(1, 22, 0));

        // Act
        var afterMidnight = gate.GetWaitBeforeRun(At(2, 0, 30));
        var nextNight = gate.GetWaitBeforeRun(At(2, 22, 0));

        // Assert
        Assert.Equal(new TimeSpan(21, 30, 0), afterMidnight);
        Assert.Null(nextNight);
    }

    private static DateTime At(int day, int hour, int minute)
    {
        return new DateTime(2000, 1, day, hour, minute, 0, DateTimeKind.Unspecified);
    }
}
