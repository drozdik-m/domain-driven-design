using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Exception handler for any type of unhandled exceptions.
/// Returns HTTP 500 Internal Server Error.
/// </summary>
public class GlobalExceptionHandler(
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : ExceptionHandler(environment)
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await WriteResponseAndLogAsync(
            logger,
            httpContext,
            exception,
            TypedResults.Problem(
                detail: GetExceptionDetail(exception),
                statusCode: StatusCodes.Status500InternalServerError,
                title: ExceptionMessages.ExceptionTitle,
                extensions: GetExtensionDataWithDetails(exception)));
    }
}
