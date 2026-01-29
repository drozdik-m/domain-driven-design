using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Mediator.Pipelines.Integrators;

/// <summary>
/// Acts as a neutral integrator that performs no operations, effectively serving as a placeholder or default integrator.
/// </summary>
public record EmptyPipelineIntegrator() : IServicePipelineIntegrator
{
    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>(ServicePipelineBuilder<TCommand, TOutput> builder)
        where TCommand : ICommand<TOutput> => builder;

    /// <inheritdoc />
    public ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>(ServicePipelineBuilder<TQuery, TOutput> builder)
        where TQuery : IQuery<TOutput> => builder;

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>(ServicePipelineBuilder<TCommand> builder)
        where TCommand : ICommand => builder;

    /// <inheritdoc />
    public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services)
        where TCommand : ICommand<TOutput> => services;

    /// <inheritdoc />
    public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services)
        where TQuery : IQuery<TOutput> => services;

    /// <inheritdoc />
    public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services)
        where TCommand : ICommand => services;
}
