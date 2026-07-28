using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Catches <see cref="BusinessRuleValidationException"/> and converts it to proper HTTP response.
/// </summary>
public class BusinessRuleValidationExceptionHandler(
    IHostEnvironment environment,
    ILogger<BusinessRuleValidationExceptionHandler> logger) : ExceptionHandler(environment)
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleValidationException validationException)
        {
            return false;
        }

        return await WriteResponseAndLogAsync(
            logger,
            httpContext,
            validationException,
            Results.ValidationProblem(
                errors: validationException.DetailsDictionary,
                detail: GetExceptionDetail(exception),
                title: ExceptionMessages.ValidationExceptionTitle,
                extensions: GetExtensionData()));
    }
}
