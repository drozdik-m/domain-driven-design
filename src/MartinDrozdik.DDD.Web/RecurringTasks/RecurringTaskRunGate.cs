using MartinDrozdik.DDD.Web.RecurringTasks.Options;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Decides whether a recurring task may start an iteration now, given its:
/// <list type="bullet">
    /// <item><see cref="RecurringTaskOptions{TTask}.RunBetween"/></item>
    /// <item>and <see cref="RecurringTaskOptions{TTask}.MaxRunsPerDay"/></item>
/// </list>
/// </summary>
/// <remarks>
/// The runs of the current day are counted in memory and start over with every restart.
/// <para>
/// The waits are differences of wall-clock times. Across a daylight saving shift the wait can be an hour off,
/// which the loop absorbs by checking again when it wakes up.
/// </para>
/// </remarks>
/// <param name="window">The daily window in which an iteration may start, or <see langword="null"/> for all day.</param>
/// <param name="maxRunsPerDay">The most iterations that may start per day, or <see langword="null"/> for no limit.</param>
internal sealed class RecurringTaskRunGate(RecurringTaskTimeWindow? window, int? maxRunsPerDay)
{
    private readonly TimeOnly _dayStart = window?.From ?? TimeOnly.MinValue;

    private DateOnly _day;
    private int _runs;

    /// <summary>
    /// Gets how long to wait before an iteration may start.
    /// </summary>
    /// <param name="localNow">The current local time.</param>
    /// <returns><see langword="null"/> when an iteration may start now, otherwise the time until it may.</returns>
    public TimeSpan? GetWaitBeforeRun(DateTime localNow)
    {
        if (window?.Contains(TimeOnly.FromDateTime(localNow)) == false)
        {
            return UntilNext(window.From, localNow);
        }

        if (maxRunsPerDay is { } limit && RunsOn(DayOf(localNow)) >= limit)
        {
            return UntilNext(_dayStart, localNow);
        }

        return null;
    }

    /// <summary>
    /// Counts an iteration starting at <paramref name="localNow"/> toward its day.
    /// </summary>
    /// <param name="localNow">The current local time.</param>
    public void RecordRun(DateTime localNow)
    {
        var day = DayOf(localNow);
        _runs = RunsOn(day) + 1;
        _day = day;
    }

    private static TimeSpan UntilNext(TimeOnly time, DateTime localNow)
    {
        var next = localNow.Date + time.ToTimeSpan();
        if (next <= localNow)
        {
            next = next.AddDays(1);
        }

        return next - localNow;
    }

    private int RunsOn(DateOnly day)
    {
        return day == _day ? _runs : 0;
    }

    private DateOnly DayOf(DateTime localNow)
    {
        var date = DateOnly.FromDateTime(localNow);
        return TimeOnly.FromDateTime(localNow) >= _dayStart ? date : date.AddDays(-1);
    }
}
