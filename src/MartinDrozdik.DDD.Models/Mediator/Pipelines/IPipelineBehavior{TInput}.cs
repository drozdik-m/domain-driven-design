using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Delegate for the next step in the pipeline.
/// </summary>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The output of the next pipeline phase.</returns>
public delegate Task<UnitResult<Error>> PipelineNextDelegate(CancellationToken cancellationToken);

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
    /// <returns>Processed output results.</returns>
    Task<UnitResult<Error>> HandleAsync(TInput input, PipelineNextDelegate next, CancellationToken cancellationToken);
}
