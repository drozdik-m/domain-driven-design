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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recurring task {TaskName} is starting an iteration. Triggered on demand: {Triggered}.")]
    internal static partial void LogIterationStarting(ILogger logger, string taskName, bool triggered);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} finished an iteration in {ElapsedMilliseconds} ms.")]
    internal static partial void LogIterationCompleted(ILogger logger, string taskName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Recurring task {TaskName} failed after {ElapsedMilliseconds} ms. The loop continues and will run again.")]
    internal static partial void LogIterationFailed(ILogger logger, Exception exception, string taskName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recurring task {TaskName} exceeded its timeout of {Timeout} and was cancelled after {ElapsedMilliseconds} ms.")]
    internal static partial void LogIterationTimedOut(ILogger logger, string taskName, TimeSpan timeout, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recurring task {TaskName} is stopping.")]
    internal static partial void LogStopping(ILogger logger, string taskName);
}
