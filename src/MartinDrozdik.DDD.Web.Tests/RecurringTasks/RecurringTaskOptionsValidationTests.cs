using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies which schedules <see cref="RecurringTaskOptionsValidation{TTask}"/> lets through to <see cref="RecurringTaskHost{TTask}"/>.
/// </summary>
/// <remarks>
/// Every wait in the host is a <see cref="CancellationTokenSource"/> armed with <see cref="CancellationTokenSource(TimeSpan, TimeProvider)"/>,
/// which accepts at most <see cref="uint.MaxValue"/> - 1 milliseconds (about 49.7 days) and throws <see cref="ArgumentOutOfRangeException"/> above that.
/// A schedule that passes validation must therefore never exceed it, otherwise the loop dies at runtime instead of the application failing at startup.
/// <see cref="RecurringTaskOptions{TTask}.MaxWait"/> caps every wait below that.
/// </remarks>
public class RecurringTaskOptionsValidationTests
{
    private static readonly TimeSpan s_maxWait = RecurringTaskOptions<ProbeTask>.MaxWait;

    private readonly RecurringTaskOptionsValidation<ProbeTask> _validation = new();

    [Fact]
    public void Default_schedule_is_valid()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask>();

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Period_that_is_not_positive_is_rejected(int seconds)
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { Period = TimeSpan.FromSeconds(seconds) };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(RecurringTaskOptions<>.Period), StringComparison.Ordinal));
    }

    [Fact]
    public void Negative_initial_delay_is_rejected()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { InitialDelay = TimeSpan.FromSeconds(-1) };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(RecurringTaskOptions<>.InitialDelay), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Timeout_that_is_set_but_not_positive_is_rejected(int seconds)
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { Timeout = TimeSpan.FromSeconds(seconds) };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(RecurringTaskOptions<>.Timeout), StringComparison.Ordinal));
    }

    [Fact]
    public void Time_window_starting_and_ending_at_the_same_time_is_rejected()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { RunBetween = new(new TimeOnly(2, 0), new TimeOnly(2, 0)) };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(RecurringTaskOptions<>.RunBetween), StringComparison.Ordinal));
    }

    [Fact]
    public void Time_window_wrapping_past_midnight_is_valid()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { RunBetween = new(new TimeOnly(22, 0), new TimeOnly(4, 0)) };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Daily_limit_that_is_set_but_not_positive_is_rejected(int maxRunsPerDay)
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { MaxRunsPerDay = maxRunsPerDay };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(RecurringTaskOptions<>.MaxRunsPerDay), StringComparison.Ordinal));
    }

    [Fact]
    public void Daily_limit_of_one_is_valid()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { MaxRunsPerDay = 1 };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void Failure_names_the_task_it_belongs_to()
    {
        // Arrange
        var options = new RecurringTaskOptions<ProbeTask> { Period = TimeSpan.Zero };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(nameof(ProbeTask), StringComparison.Ordinal));
    }

    [Fact]
    public void Failure_names_a_generic_task_with_its_type_arguments()
    {
        // Arrange
        var validation = new RecurringTaskOptionsValidation<GenericProbeTask<int>>();
        var options = new RecurringTaskOptions<GenericProbeTask<int>> { Period = TimeSpan.Zero };

        // Act
        var result = validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("GenericProbeTask<Int32>", StringComparison.Ordinal));
    }

    [Fact]
    public void Longest_allowed_wait_is_valid_and_a_timer_can_wait_that_long()
    {
        // Arrange
        // Guards the upper bound from being drawn tighter than documented, or looser than a timer can wait
        var options = new RecurringTaskOptions<ProbeTask>
        {
            InitialDelay = s_maxWait,
            Period = s_maxWait,
            Timeout = s_maxWait,
        };

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded, result.FailureMessage);
        Assert.Equal(TimeSpan.FromDays(45), s_maxWait);
        Assert.Null(Record.Exception(() => new CancellationTokenSource(s_maxWait, TimeProvider.System).Dispose()));
    }

    [Theory]
    [InlineData(nameof(RecurringTaskOptions<>.InitialDelay))]
    [InlineData(nameof(RecurringTaskOptions<>.Period))]
    [InlineData(nameof(RecurringTaskOptions<>.Timeout))]
    public void Wait_longer_than_the_longest_allowed_is_rejected(string property)
    {
        // Arrange
        var tooLong = s_maxWait + TimeSpan.FromMilliseconds(1);
        var options = new RecurringTaskOptions<ProbeTask>();
        switch (property)
        {
            case nameof(RecurringTaskOptions<>.InitialDelay):
                options.InitialDelay = tooLong;
                break;
            case nameof(RecurringTaskOptions<>.Period):
                options.Period = tooLong;
                break;
            default:
                options.Timeout = tooLong;
                break;
        }

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        Assert.True(result.Failed, $"A {property} of {tooLong} passed validation although the longest allowed wait is {s_maxWait}.");
        Assert.Contains(result.Failures, failure => failure.Contains(property, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(nameof(RecurringTaskOptions<>.InitialDelay))]
    [InlineData(nameof(RecurringTaskOptions<>.Period))]
    [InlineData(nameof(RecurringTaskOptions<>.Timeout))]
    public void Wait_longer_than_a_timer_supports_is_rejected(string property)
    {
        // Arrange
        // 60 days: an "every two months" schedule, more than a timer can wait at all
        var tooLong = TimeSpan.FromDays(60);
        var options = new RecurringTaskOptions<ProbeTask>();
        switch (property)
        {
            case nameof(RecurringTaskOptions<>.InitialDelay):
                options.InitialDelay = tooLong;
                break;
            case nameof(RecurringTaskOptions<>.Period):
                options.Period = tooLong;
                break;
            default:
                options.Timeout = tooLong;
                break;
        }

        // Act
        var result = _validation.Validate(null, options);

        // Assert
        // Were this to pass validation, the host would throw ArgumentOutOfRangeException out of
        // new CancellationTokenSource(delay, timeProvider), which stops the whole application
        Assert.Throws<ArgumentOutOfRangeException>(() => new CancellationTokenSource(tooLong, TimeProvider.System));
        Assert.True(result.Failed, $"A {property} of {tooLong} passed validation although no timer can wait that long.");
        Assert.Contains(result.Failures, failure => failure.Contains(property, StringComparison.Ordinal));
    }

    private sealed class ProbeTask : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

#pragma warning disable S2326 // Unused type parameters - only the name it gives the task is under test
    private sealed class GenericProbeTask<T> : IRecurringTask
#pragma warning restore S2326
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
