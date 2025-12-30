using MartinDrozdik.DDD.Models.Exceptions;
using MartinDrozdik.DDD.Models.Extensions;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : ExceptionHandler
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var businessException = validationException.Errors.GetException();

        ExceptionHandlerLogging.LogError(logger, context, exception);

        await Results.ValidationProblem(
            title: "A validation error occurred while processing the request.",
            errors: businessException.DetailsDictionary,
            extensions: GetExtensionData(context, exception)
        ).ExecuteAsync(context);

        return true;
    }
}
