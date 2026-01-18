using MartinDrozdik.DDD.Exceptions;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public class BusinessRuleValidationExceptionHandler(ILogger<BusinessRuleValidationExceptionHandler> logger) : ExceptionHandler
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleValidationException validationException)
        {
            return false;
        }

        MiddlewareLogging.LogError(logger, context, validationException);

        await Results.ValidationProblem(
            title: "A validation error occurred while processing the request.",
            errors: validationException.DetailsDictionary,
            extensions: GetExtensionData(context, validationException)
        ).ExecuteAsync(context);

        return true;
    }
}
