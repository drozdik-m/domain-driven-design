using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Mediator.Pipelines.Integrators;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Mediator.Pipelines.Logging;

/// <summary>
/// <see cref="IServicePipelineIntegrator"/> for adding validation pipelines:
/// <list type="bullet">
/// <item><see cref="LoggingPipeline{TRequest}"/></item>
/// <item><see cref="LoggingPipeline{TRequest,TOutput}"/></item>
/// </list>
/// </summary>
public class LoggingPipelineIntegrator : IServicePipelineIntegrator
{
    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>(ServicePipelineBuilder<TCommand, TOutput> builder)
        where TCommand : ICommand<TOutput>
    {
        builder.Add<LoggingPipeline<TCommand, TOutput>>();
        return builder;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>(ServicePipelineBuilder<TQuery, TOutput> builder)
        where TQuery : IQuery<TOutput>
    {
        builder.Add<LoggingPipeline<TQuery, TOutput>>();
        return builder;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>(ServicePipelineBuilder<TCommand> builder)
        where TCommand : ICommand
    {
        builder.Add<LoggingPipeline<TCommand>>();
        return builder;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services)
        where TCommand : ICommand<TOutput>
    {
        services.AddScoped<LoggingPipeline<TCommand, TOutput>>();
        return services;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services)
        where TQuery : IQuery<TOutput>
    {
        services.AddScoped<LoggingPipeline<TQuery, TOutput>>();
        return services;
    }

    /// <inheritdoc />
    public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services)
        where TCommand : ICommand
    {
        services.AddScoped<LoggingPipeline<TCommand>>();
        return services;
    }
}
