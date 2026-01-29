using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Mediator.Pipelines;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Mediator.Pipelines.Logging;

/// <summary>
/// Pipeline behavior for logging requests and responses.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <param name="logger">The target logger for events.</param>
public class LoggingPipeline<TRequest>(ILogger<LoggingPipeline<TRequest>> logger)
    : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    /// <inheritdoc />
    public async Task HandleAsync(TRequest input, PipelineNextDelegate next, CancellationToken cancellationToken)
    {
        try
        {
            await next(cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Request of type {RequestType} processed successfully: {@Request}", typeof(TRequest).Name, input);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "An error occurred while processing request of type {RequestType}: {@Request}", typeof(TRequest).Name, input);
            }

            throw;
        }
    }
}
