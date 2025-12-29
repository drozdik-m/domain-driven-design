using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Represents an empty pipeline that does nothing and returns the next step in the pipeline.
/// </summary>
/// <typeparam name="TInput">Input request object.</typeparam>
/// <typeparam name="TOutput">Result of the operation.</typeparam>
public sealed class EmptyPipeline<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
{
    private EmptyPipeline()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the empty pipeline.
    /// </summary>
    public static EmptyPipeline<TInput, TOutput> Instance { get; } = new EmptyPipeline<TInput, TOutput>();

    /// <inheritdoc />
    public Task<TOutput> HandleAsync(TInput input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken)
        => next(cancellationToken);
}
