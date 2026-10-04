namespace MartinDrozdik.DDD.Web.RecurringTasks.Options;

/// <summary>
/// A daily window of local time in which a recurring task may start an iteration.
/// </summary>
/// <remarks>
/// A window may wrap past midnight: <c>new(new TimeOnly(22, 0), new TimeOnly(4, 0))</c> is a nightly window from 22:00 to 04:00.
/// <paramref name="From"/> and <paramref name="To"/> must differ.
/// </remarks>
/// <param name="From">The first moment of the window, inclusive.</param>
/// <param name="To">The end of the window, exclusive.</param>
public sealed record RecurringTaskTimeWindow(TimeOnly From, TimeOnly To)
{
    /// <summary>
    /// Determines whether <paramref name="time"/> lies inside the window.
    /// </summary>
    /// <param name="time">The local time of day to check.</param>
    /// <returns><see langword="true"/> when an iteration may start at <paramref name="time"/>.</returns>
    internal bool Contains(TimeOnly time)
    {
        return time.IsBetween(From, To);
    }
}
