using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Source-generated log messages emitted by <see cref="RecurringTaskHost{TTask}"/>.
/// </summary>
internal static partial class RecurringTaskLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} is disabled and will not run.")]
    internal static partial void LogDisabled(ILogger logger, string taskName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} scheduled with an initial delay of {InitialDelay} and a period of {Period}.")]
    internal static partial void LogScheduled(ILogger logger, string taskName, TimeSpan initialDelay, TimeSpan period);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} starts iterations only between {From} and {To} local time.")]
    internal static partial void LogTimeWindow(ILogger logger, string taskName, TimeOnly from, TimeOnly to);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} starts at most {MaxRunsPerDay} iterations per day, counted in memory.")]
    internal static partial void LogDailyLimit(ILogger logger, string taskName, int maxRunsPerDay);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recurring task {TaskName} may not run now, being outside its time window or over its daily limit. Next attempt in {Wait}.")]
    internal static partial void LogRunPostponed(ILogger logger, string taskName, TimeSpan wait);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recurring task {TaskName} is starting an iteration. Triggered on demand: {Triggered}.")]
    internal static partial void LogIterationStarting(ILogger logger, string taskName, bool triggered);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recurring task {TaskName} finished an iteration in {ElapsedMilliseconds} ms.")]
    internal static partial void LogIterationCompleted(ILogger logger, string taskName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Recurring task {TaskName} failed after {ElapsedMilliseconds} ms. The loop continues and will run again.")]
    internal static partial void LogIterationFailed(ILogger logger, Exception exception, string taskName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recurring task {TaskName} threw while the application was stopping, {ElapsedMilliseconds} ms after the iteration started. Most likely the shutdown cancelled it, so the loop ends.")]
    internal static partial void LogIterationFailedWhileStopping(ILogger logger, Exception exception, string taskName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recurring task {TaskName} exceeded its timeout of {Timeout}. Its cancellation token was cancelled and the next iteration waits until this one returns.")]
    internal static partial void LogTimeoutElapsed(ILogger logger, string taskName, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} ended an iteration {ElapsedMilliseconds} ms after it started, cancelled by its timeout of {Timeout}.")]
    internal static partial void LogIterationEndedAfterTimeout(ILogger logger, string taskName, TimeSpan timeout, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} is stopping.")]
    internal static partial void LogStopping(ILogger logger, string taskName);
}
