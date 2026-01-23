using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares.Logging;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;

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
