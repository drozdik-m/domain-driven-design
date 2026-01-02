using System.Diagnostics;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public static partial class MiddlewareLogging
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


    /// <summary>
    /// Logs HTTP request details with provided HTTP context.
    /// </summary>
    public static void LogRequestInformation(ILogger logger, HttpContext context)
    {
        var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var method = context.Request.Method;
        var traceId = Activity.Current?.TraceId;
        var traceIdString = traceId?.ToString() ?? "N/A";

        // Log details
        LogRequestInformation(logger, method, fullUrl, traceIdString, Environment.MachineName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing HTTP request {Method} {FullUrl}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogRequestInformation(ILogger logger, string method, string fullUrl, string traceId, string machineName);

    /// <summary>
    /// Logs successful HTTP response details with provided HTTP context.
    /// </summary>
    public static void LogSuccessResponseInformation(ILogger logger, HttpContext context)
    {
        var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var method = context.Request.Method;
        var traceId = Activity.Current?.TraceId;
        var traceIdString = traceId?.ToString() ?? "N/A";
        var statusCode = context.Response.StatusCode;

        // Log details
        LogSuccessResponseInformation(logger, method, fullUrl, statusCode, traceIdString, Environment.MachineName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed HTTP request {Method} {FullUrl} => {StatusCode}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogSuccessResponseInformation(ILogger logger, string method, string fullUrl, int statusCode, string traceId, string machineName);
}
