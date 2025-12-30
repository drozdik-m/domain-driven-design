using System.Diagnostics;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public static partial class ExceptionHandlerLogging
{
    /// <summary>
    /// Logs error details with provided HTTP context.
    /// </summary>
    public static void LogError(ILogger logger, HttpContext context, Exception? exception)
    {
        var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var method = context.Request.Method;
        var traceId = Activity.Current?.TraceId;
        var traceIdString = traceId?.ToString() ?? "N/A";
        var statusCode = context.Response.StatusCode;

        // Log details
        LogError(logger, exception, method, fullUrl, statusCode, traceIdString, Environment.MachineName);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing request {Method} {FullUrl} => {StatusCode}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogError(ILogger logger, Exception? exception, string method, string fullUrl, int statusCode, string traceId, string machineName);
}
