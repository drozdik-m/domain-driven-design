using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Represents an empty pipeline that does nothing and returns the next step in the pipeline.
/// </summary>
/// <typeparam name="TInput">Input request object.</typeparam>
public sealed class EmptyPipeline<TInput> : IPipelineBehavior<TInput>
{
    private EmptyPipeline()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the empty pipeline.
    /// </summary>
    public static EmptyPipeline<TInput> Instance { get; } = new EmptyPipeline<TInput>();

    /// <inheritdoc />
    public Task HandleAsync(TInput input, PipelineNextDelegate next, CancellationToken cancellationToken)
        => next(cancellationToken);
}
