using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web.Middlewares.Exceptions;
using MartinDrozdik.DDD.Web.Tests.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

/// <summary>
/// Catches <see cref="BusinessRuleValidationException"/> and converts it to proper HTTP response.
/// </summary>
public class BusinessRuleValidationExceptionHandler(ILogger<BusinessRuleValidationExceptionHandler> logger) : ExceptionHandler
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

        RequestLogging.LogError(logger, httpContext, validationException);

        await Results.ValidationProblem(
            errors: validationException.DetailsDictionary,
            title: "A validation error occurred while processing the request.",
            extensions: GetExtensionData()).ExecuteAsync(httpContext);

        return true;
    }
}
