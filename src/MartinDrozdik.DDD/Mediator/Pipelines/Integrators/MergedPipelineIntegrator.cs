using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Mediator.Pipelines.Integrators;

/// <summary>
/// Merges multiple <see cref="IServicePipelineIntegrator"/>s into a single integrator.
/// </summary>
/// <param name="integrators">Integrators to merge.</param>
public record MergedPipelineIntegrator(params IServicePipelineIntegrator[] integrators) : IServicePipelineIntegrator
{
    /// <summary>
    /// Merges additional <see cref="IServicePipelineIntegrator"/>s into the current integrator.
    /// </summary>
    /// <param name="integrators">Additional integrators.</param>
    /// <returns>New merged integrator.</returns>
    public MergedPipelineIntegrator Merge(params IServicePipelineIntegrator[] integrators)
    {
        return this with { integrators = [.. this.integrators, .. integrators] };
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>(ServicePipelineBuilder<TCommand, TOutput> builder)
        where TCommand : ICommand<TOutput>
    {
        foreach (var integrator in integrators)
        {
            integrator.BuildCommandPipeline(builder);
        }

        return builder;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>(ServicePipelineBuilder<TQuery, TOutput> builder)
        where TQuery : IQuery<TOutput>
    {
        foreach (var integrator in integrators)
        {
            integrator.BuildQueryPipeline(builder);
        }

        return builder;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>(ServicePipelineBuilder<TCommand> builder)
        where TCommand : ICommand
    {
        foreach (var integrator in integrators)
        {
            integrator.BuildUnitCommandPipeline(builder);
        }

        return builder;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services)
        where TCommand : ICommand<TOutput>
    {
        foreach (var integrator in integrators)
        {
            services = integrator.RegisterCommandPipeline<TCommand, TOutput>(services);
        }

        return services;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services)
        where TQuery : IQuery<TOutput>
    {
        foreach (var integrator in integrators)
        {
            services = integrator.RegisterQueryPipeline<TQuery, TOutput>(services);
        }

        return services;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services)
        where TCommand : ICommand
    {
        foreach (var integrator in integrators)
        {
            services = integrator.RegisterUnitCommandPipeline<TCommand>(services);
        }

        return services;
    }
}
