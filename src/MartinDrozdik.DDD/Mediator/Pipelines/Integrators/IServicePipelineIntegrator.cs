using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Mediator.Pipelines.Integrators;

/// <summary>
/// Defines methods for constructing service pipelines to process queries and commands within an application.
/// </summary>
public interface IServicePipelineIntegrator
{
    /// <summary>
    /// Registers pipeline behaviors for processing queries.
    /// </summary>
    /// <typeparam name="TQuery">Type of the query.</typeparam>
    /// <typeparam name="TOutput">Type of the output.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services)
        where TQuery : IQuery<TOutput>;

    /// <summary>
    /// Builds a service pipeline for processing queries.
    /// </summary>
    /// <param name="builder">The pipeline builder to expand.</param>
    /// <typeparam name="TQuery">Type of the query.</typeparam>
    /// <typeparam name="TOutput">Type of the output.</typeparam>
    /// <returns>The pipeline.</returns>
    public abstract ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>(ServicePipelineBuilder<TQuery, TOutput> builder)
        where TQuery : IQuery<TOutput>;

    /// <summary>
    /// Registers pipeline behaviors for processing commands.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <typeparam name="TOutput">Type of the output.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services)
        where TCommand : ICommand<TOutput>;

    /// <summary>
    /// Builds a service pipeline for processing commands.
    /// </summary>
    /// <param name="builder">The pipeline builder to expand.</param>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <typeparam name="TOutput">Type of the output.</typeparam>
    /// <returns>The pipeline.</returns>
    public abstract ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>(ServicePipelineBuilder<TCommand, TOutput> builder)
        where TCommand : ICommand<TOutput>;

    /// <summary>
    /// Registers pipeline behaviors for processing unit commands.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services)
        where TCommand : ICommand;

    /// <summary>
    /// Builds a service pipeline for processing unit commands.
    /// </summary>
    /// <param name="builder">The pipeline builder to expand.</param>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <returns>The pipeline.</returns>
    public abstract ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>(ServicePipelineBuilder<TCommand> builder)
        where TCommand : ICommand;
}
