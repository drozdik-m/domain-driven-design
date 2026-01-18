using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Mediator.Pipelines;

namespace MartinDrozdik.DDD.Demo.Requests.Pipelines;

public class LoggingPipeline<TRequest, TOutput>(ILogger<LoggingPipeline<TRequest, TOutput>> logger)
    : IPipelineBehavior<TRequest, TOutput>
    where TRequest : IRequest<TOutput>
{
    /// <inheritdoc />
    public async Task<TOutput> HandleAsync(TRequest input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken)
    {
        try
        {
            var result = await next(cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Request of type {RequestType} processed successfully: {@Request} -> {@Response}", typeof(TRequest).Name, input, result);
            }
            return result;
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
