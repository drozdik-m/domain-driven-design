using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Represents a pipeline behavior in the Mediator pattern.
/// Is used to apply cross-cutting concerns such as logging, validation, or transaction management.
/// </summary>
/// <typeparam name="TInput">Input request object.</typeparam>
/// <typeparam name="TOutput">Result of the operation</typeparam>
public interface IPipelineBehavior<in TInput, TOutput>
{
    /// <summary>
    /// Handles the input with added pipeline behaviour and invokes the next step in the pipeline.
    /// </summary>
    /// <param name="input">Pipeline input</param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TOutput> HandleAsync(TInput input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next step in the pipeline.
/// </summary>
/// <typeparam name="TOutput">Output of the next step.</typeparam>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The output of the next pipeline phase.</returns>
public delegate Task<TOutput> PipelineNextDelegate<TOutput>(CancellationToken cancellationToken);
