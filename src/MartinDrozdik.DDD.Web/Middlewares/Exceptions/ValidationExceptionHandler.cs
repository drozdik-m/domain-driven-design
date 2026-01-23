using FluentValidation;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Tests.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Catches FluentValidations' <see cref="ValidationException"/> and converts it to proper HTTP response.
/// </summary>
public class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : ExceptionHandler
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

        RequestLogging.LogError(logger, httpContext, exception);

        await Results.ValidationProblem(
            errors: businessException.DetailsDictionary,
            title: "A validation error occurred while processing the request.",
            extensions: GetExtensionData()).ExecuteAsync(httpContext);

        return true;
    }
}
