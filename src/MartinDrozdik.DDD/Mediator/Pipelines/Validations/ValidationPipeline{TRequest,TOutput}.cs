namespace MartinDrozdik.DDD.Mediator.Pipelines.Validations;

/// <summary>
/// Pipeline that validates requests implementing <see cref="IValidatedMessage{TMessage}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TOutput">Type of the output.</typeparam>
public class ValidationPipeline<TRequest, TOutput>() : ValidationPipeline<TRequest>, IPipelineBehavior<TRequest, TOutput>
    where TRequest : IRequest<TOutput>
{
    /// <inheritdoc />
    public Task<TOutput> HandleAsync(TRequest input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken)
    {
        ThrowIfInvalid(input);
        return next(cancellationToken);
    }
}
