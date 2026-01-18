namespace MartinDrozdik.DDD.Mediator.Pipelines;

/// <summary>
/// Builds a pipeline of behaviors that can process input and produce output.
/// </summary>
/// <typeparam name="TInput">Type of the input.</typeparam>
/// <typeparam name="TOutput">Type of the output.</typeparam>
public class PipelineBuilder<TInput, TOutput>
{
    private readonly List<IPipelineBehavior<TInput, TOutput>> _pipelines = [];

    /// <summary>
    /// Adds a behaviour step to the pipeline.
    /// </summary>
    /// <param name="step">The step to invoke.</param>
    /// <returns>This for chaining.</returns>
    public PipelineBuilder<TInput, TOutput> Add(IPipelineBehavior<TInput, TOutput> step)
    {
        _pipelines.Add(step);
        return this;
    }

    /// <summary>
    /// Returns the entry point of the pipeline.
    /// </summary>
    /// <returns>Pipeline behavior that represents the entry point of the pipeline.</returns>
    public IPipelineBehavior<TInput, TOutput> Build()
    {
        if (_pipelines.Count == 0)
        {
            return EmptyPipeline<TInput, TOutput>.Instance;
        }

        // Reverse the order of the pipelines to ensure they are executed in the correct order
        // Make a copy
        var reversedPipelines = _pipelines.AsEnumerable().Reverse().ToArray();

        // Create the pipeline with the behaviors
        return new Pipeline<TInput, TOutput>(reversedPipelines);
    }
}
