using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Builds a pipeline of behaviors that can process input and produce output.
/// Build is done via <see cref="IServiceProvider"/> to enable dependency injection for the behaviors.
/// </summary>
/// <typeparam name="TInput">Type of the input.</typeparam>
public class ServicePipelineBuilder<TInput>
{
    private readonly List<Type> _pipelineTypes = [];

    /// <summary>
    /// Adds a behaviour step to the pipeline.
    /// </summary>
    /// <typeparam name="TPipelineBehaviour">Type of the pipeline to build and invoke.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServicePipelineBuilder<TInput> Add<TPipelineBehaviour>()
        where TPipelineBehaviour : IPipelineBehavior<TInput>
    {
        return Add(typeof(TPipelineBehaviour));
    }

    /// <summary>
    /// Adds a behaviour step to the pipeline.
    /// </summary>
    /// <param name="pipelineType">Type of the pipeline to build and invoke.</param>
    /// <returns>This for chaining.</returns>
    public ServicePipelineBuilder<TInput> Add(Type pipelineType)
    {
        // Validate the pipeline type
        ArgumentNullException.ThrowIfNull(pipelineType);
        if (!typeof(IPipelineBehavior<TInput>).IsAssignableFrom(pipelineType))
        {
            throw new ArgumentException($"Type {pipelineType.Name} does not implement {nameof(IPipelineBehavior<TInput>)}", nameof(pipelineType));
        }

        // Add the pipeline to the start of the list to ensure it is executed first
        _pipelineTypes.Insert(0, pipelineType);
        return this;
    }

    /// <summary>
    /// Returns the entry point of the pipeline.
    /// </summary>
    /// <param name="serviceProvider">Service provider to resolve the pipeline behaviors.</param>
    /// <returns>Pipeline behavior that represents the entry point of the pipeline.</returns>
    public IPipelineBehavior<TInput> Build(IServiceProvider serviceProvider)
    {
        if (_pipelineTypes.Count == 0)
        {
            return EmptyPipeline<TInput>.Instance;
        }

        // Resolve the pipeline behaviors from the service provider
        var reversedPipelines = _pipelineTypes
            .Select(type => (IPipelineBehavior<TInput>)serviceProvider.GetRequiredService(type))
            .ToArray();

        // Create the pipeline with the behaviors
        return new Pipeline<TInput>(reversedPipelines);
    }
}
