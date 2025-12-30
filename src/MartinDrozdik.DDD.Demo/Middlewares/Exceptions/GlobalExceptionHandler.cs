using System.Diagnostics;
using MartinDrozdik.DDD.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : ExceptionHandler
{
    /// <inheritdoc />
    public override async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ExceptionHandlerLogging.LogError(logger, context, exception);

        await Results.Problem(
            title: "An error occurred while processing the request.",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: GetExtensionDataWithDetails(context, exception)
        ).ExecuteAsync(context);

        return true;
    }
}
