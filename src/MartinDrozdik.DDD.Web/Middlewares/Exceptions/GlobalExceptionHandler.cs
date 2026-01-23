using MartinDrozdik.DDD.Web.Tests.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Exception handler for any type of unhandled exceptions.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : ExceptionHandler
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        RequestLogging.LogError(logger, httpContext, exception);

        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An error occurred while processing the request.",
            extensions: GetExtensionDataWithDetails(exception)).ExecuteAsync(httpContext);

        return true;
    }
}
