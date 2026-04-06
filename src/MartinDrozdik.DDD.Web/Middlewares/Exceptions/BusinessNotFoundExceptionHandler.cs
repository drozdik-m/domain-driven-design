using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web.Tests.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;
/*
/// <summary>
/// Catches <see cref="BusinessNotFoundException"/> and converts it to proper HTTP response.
/// </summary>
public class BusinessNotFoundExceptionHandler(
    IHostEnvironment environment,
    ILogger<BusinessNotFoundExceptionHandler> logger) : ExceptionHandler(environment)
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessNotFoundException validationException)
        {
            return false;
        }

        RequestLogging.LogError(logger, httpContext, validationException);

        await Results.ValidationProblem(
            errors: validationException.DetailsDictionary,
            detail: GetExceptionDetail(exception),
            title: ExceptionLocalization.ValidationExceptionTitle,
            extensions: GetExtensionData()).ExecuteAsync(httpContext);

        return true;
    }
}
*/
