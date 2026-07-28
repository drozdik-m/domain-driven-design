using FluentValidation;
using MartinDrozdik.DDD.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Catches FluentValidations' <see cref="ValidationException"/> and converts it to proper HTTP response.
/// </summary>
public class ValidationExceptionHandler(
    IHostEnvironment environment,
    ILogger<ValidationExceptionHandler> logger) : ExceptionHandler(environment)
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var businessException = validationException.Errors.GetException();

        return await WriteResponseAndLogAsync(
            logger,
            httpContext,
            exception,
            Results.ValidationProblem(
                errors: businessException.DetailsDictionary,
                detail: GetExceptionDetail(exception),
                title: ExceptionMessages.ValidationExceptionTitle,
                extensions: GetExtensionData()));
    }
}
