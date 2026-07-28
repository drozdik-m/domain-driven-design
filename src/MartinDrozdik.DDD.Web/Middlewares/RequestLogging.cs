using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares;

/// <summary>
/// Static class for various logging related to HTTP requests.
/// </summary>
public static partial class RequestLogging
{
    /// <summary>
    /// Logs the response of a finished HTTP request, choosing the log level by the response status code:
    /// <list type="bullet">
    ///     <item>1xx, 2xx and 3xx (informational, success, redirection) — <see cref="LogLevel.Information"/></item>
    ///     <item>4xx (client error) — <see cref="LogLevel.Warning"/></item>
    ///     <item>5xx (server error) — <see cref="LogLevel.Error"/></item>
    /// </list>
    /// </summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="context">Source <see cref="HttpContext"/>.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void LogResponseInformation(ILogger logger, HttpContext context, Exception? exception = null)
    {
        var statusCode = context.Response.StatusCode;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogError(logger, context, exception);
        }
        else if (statusCode >= StatusCodes.Status400BadRequest)
        {
            LogClientErrorResponseInformation(logger, context, exception);
        }
        else
        {
            LogSuccessResponseInformation(logger, context);
        }
    }

    /// <summary>
    /// Logs error details with provided <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="context">Source <see cref="HttpContext"/>.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void LogError(ILogger logger, HttpContext context, Exception? exception)
    {
        var fullUrl = GetRequestUrl(context.Request);
        var method = context.Request.Method;
        var traceId = GetTraceId();
        var statusCode = context.Response.StatusCode;

        // Log details
        LogError(logger, exception, method, fullUrl, statusCode, traceId, Environment.MachineName);
    }

    /// <summary>
    /// Logs HTTP request details with provided <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="context">Source <see cref="HttpContext"/>.</param>
    public static void LogRequestInformation(ILogger logger, HttpContext context)
    {
        var fullUrl = GetRequestUrl(context.Request);
        var method = context.Request.Method;
        var traceId = GetTraceId();

        // Log details
        LogRequestInformation(logger, method, fullUrl, traceId, Environment.MachineName);
    }

    /// <summary>
    /// Logs successful HTTP response details with provided HTTP context.
    /// </summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="context">Source <see cref="HttpContext"/>.</param>
    public static void LogSuccessResponseInformation(ILogger logger, HttpContext context)
    {
        var fullUrl = GetRequestUrl(context.Request);
        var method = context.Request.Method;
        var traceId = GetTraceId();
        var statusCode = context.Response.StatusCode;

        // Log details
        LogSuccessResponseInformation(logger, method, fullUrl, statusCode, traceId, Environment.MachineName);
    }

    /// <summary>
    /// Logs a client-error (4xx) HTTP response with provided <see cref="HttpContext"/>.
    /// A client fault is not an application failure, so it is logged at <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="context">Source <see cref="HttpContext"/>.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void LogClientErrorResponseInformation(ILogger logger, HttpContext context, Exception? exception)
    {
        var fullUrl = GetRequestUrl(context.Request);
        var method = context.Request.Method;
        var traceId = GetTraceId();
        var statusCode = context.Response.StatusCode;

        // Log details
        LogClientErrorResponseInformation(logger, exception, method, fullUrl, statusCode, traceId, Environment.MachineName);
    }

    /// <summary>
    /// Gets the full URL from the HTTP context.
    /// </summary>
    /// <param name="request">Source request.</param>
    /// <returns>Full url of the request.</returns>
    /// <example>https://example.com/api/resource?id=123 .</example>
    private static string GetRequestUrl(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    }

    /// <summary>
    /// Gets the trace ID from the HTTP context.
    /// </summary>
    /// <returns>Returns trace id as a string.</returns>
    private static string GetTraceId()
    {
        var traceId = Activity.Current?.TraceId;
        return traceId?.ToString() ?? "N/A";
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing request {Method} {FullUrl} => {StatusCode}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogError(ILogger logger, Exception? exception, string method, string fullUrl, int statusCode, string traceId, string machineName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing HTTP request {Method} {FullUrl}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogRequestInformation(ILogger logger, string method, string fullUrl, string traceId, string machineName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed HTTP request {Method} {FullUrl} => {StatusCode}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogSuccessResponseInformation(ILogger logger, string method, string fullUrl, int statusCode, string traceId, string machineName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Client error processing request {Method} {FullUrl} => {StatusCode}.\nTraceId: {TraceId}.\nMachine: {MachineName}")]
    private static partial void LogClientErrorResponseInformation(ILogger logger, Exception? exception, string method, string fullUrl, int statusCode, string traceId, string machineName);
}
