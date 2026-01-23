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
}
