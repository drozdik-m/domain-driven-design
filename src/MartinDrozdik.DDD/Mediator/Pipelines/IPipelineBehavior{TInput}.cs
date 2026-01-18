namespace MartinDrozdik.DDD.Mediator.Pipelines;

/// <summary>
/// Delegate for the next step in the pipeline.
/// </summary>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns><see cref="Task"/>.</returns>
public delegate Task PipelineNextDelegate(CancellationToken cancellationToken);

/// <summary>
/// Represents a pipeline behavior in the Mediator pattern.
/// Is used to apply cross-cutting concerns such as logging, validation, or transaction management.
/// </summary>
/// <typeparam name="TInput">Input request object.</typeparam>
public interface IPipelineBehavior<in TInput>
{
    /// <summary>
    /// Handles the input with added pipeline behaviour and invokes the next step in the pipeline.
    /// </summary>
    /// <param name="input">Pipeline input.</param>
    /// <param name="next">The next delegate that should be handled according to this behaviours logic.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task HandleAsync(TInput input, PipelineNextDelegate next, CancellationToken cancellationToken);
}
