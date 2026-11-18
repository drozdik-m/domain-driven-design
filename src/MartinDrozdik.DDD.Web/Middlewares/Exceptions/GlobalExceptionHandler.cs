using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Exception handler for any type of unhandled exceptions.
/// Returns HTTP 500 Internal Server Error.
/// <para>
/// Outside Development the response hides the message of any exception other than a <see cref="BusinessRuleException"/>,
/// see <see cref="ExceptionHandler.GetSafeExceptionDetail(Exception)"/>.
/// </para>
/// </summary>
public class GlobalExceptionHandler(
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : ExceptionHandler(environment)
{
    /// <inheritdoc />
    public override ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return WriteResponseAndLogAsync(
            logger,
            httpContext,
            exception,
            TypedResults.Problem(
                detail: GetSafeExceptionDetail(exception),
                statusCode: StatusCodes.Status500InternalServerError,
                title: ExceptionMessages.ExceptionTitle,
                extensions: GetExtensionDataWithDetails(exception)));
    }
}
