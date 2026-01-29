using MartinDrozdik.DDD.Web.Tests.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Logging;

/// <summary>
/// Middleware for logging request and response information.
/// </summary>
/// <param name="next">The next middleware.</param>
/// <param name="logger">The target logger for request-response information.</param>
public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;

    /// <summary>
    /// Logs basic request and response information.
    /// Keeps context as-is.
    /// </summary>
    /// <param name="context">The context as source of loggin information.</param>
    /// <returns><see cref="Task"/>.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request
        RequestLogging.LogRequestInformation(_logger, context);

        // Call the next middleware in the pipeline
        try
        {
            await _next(context);

            // Check success
            if ((context.Response.StatusCode >= 200 && context.Response.StatusCode < 300) ||
                context.Response.StatusCode == 404)
            {
                RequestLogging.LogSuccessResponseInformation(_logger, context);
            }
            else
            {
                RequestLogging.LogError(_logger, context, exception: null);
            }
        }
        catch (Exception e)
        {
            RequestLogging.LogError(_logger, context, e);
            throw;
        }
    }
}
