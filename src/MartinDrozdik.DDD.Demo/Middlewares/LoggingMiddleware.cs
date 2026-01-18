using MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

namespace MartinDrozdik.DDD.Demo.Middlewares;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request
        MiddlewareLogging.LogRequestInformation(_logger, context);

        // Call the next middleware in the pipeline
        try
        {
            await _next(context);

            // Check success
            if ((context.Response.StatusCode >= 200 && context.Response.StatusCode < 300) ||
                context.Response.StatusCode == 404)
            {
                MiddlewareLogging.LogSuccessResponseInformation(_logger, context);
            }
            else
            {
                MiddlewareLogging.LogError(_logger, context, exception: null);
            }
        }
        catch (Exception e)
        {
            MiddlewareLogging.LogError(_logger, context, e);
            throw;
        }
    }
}
