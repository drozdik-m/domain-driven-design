using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Mediator.Pipelines.Integrators;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Mediator;

/// <summary>
/// Configuration class for the Mediator service.
/// </summary>
/// <param name="services">Services to register handlers to.</param>
public class ServiceMediatorConfig(IServiceCollection services)
{
    /// <inheritdoc cref="WithQuery{TQuery, TResponse, THandler}(ServicePipelineBuilder{TQuery, TResponse})"/>
    public ServiceMediatorConfig WithQuery<TQuery, TResponse, THandler>()
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<IQueryHandler<TQuery, TResponse>, THandler>();
        return this;
    }

    /// <inheritdoc cref="WithQuery{TQuery, TResponse, THandler}(ServicePipelineBuilder{TQuery, TResponse})"/>
    public ServiceMediatorConfig WithQuery<TQuery, TResponse, THandler>(IServicePipelineIntegrator integrator)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        integrator.RegisterQueryPipeline<TQuery, TResponse>(services);

        var builder = new ServicePipelineBuilder<TQuery, TResponse>();
        var pipelineBuilder = integrator.BuildQueryPipeline(builder);

        return WithQuery<TQuery, TResponse, THandler>(pipelineBuilder);
    }

    /// <summary>
    /// Registers a query handler for a specific query type and response type.
    /// </summary>
    /// <param name="pipelineBuilder">Pipeline builder with added pipeline types.</param>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResponse">The type of the query response.</typeparam>
    /// <typeparam name="THandler">The type of the query handler.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithQuery<TQuery, TResponse, THandler>(ServicePipelineBuilder<TQuery, TResponse> pipelineBuilder)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        WithQuery<TQuery, TResponse, THandler>();
        services.AddScoped(pipelineBuilder.Build);
        return this;
    }

    /// <inheritdoc cref="WithCommand{TCommand, TResponse, THandler}(ServicePipelineBuilder{TCommand, TResponse})"/>/>
    public ServiceMediatorConfig WithCommand<TCommand, TResponse, THandler>()
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<ICommandHandler<TCommand, TResponse>, THandler>();
        return this;
    }

    /// <inheritdoc cref="WithCommand{TCommand, TResponse, THandler}(ServicePipelineBuilder{TCommand, TResponse})"/>/>
    public ServiceMediatorConfig WithCommand<TCommand, TResponse, THandler>(IServicePipelineIntegrator integrator)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        integrator.RegisterCommandPipeline<TCommand, TResponse>(services);

        var builder = new ServicePipelineBuilder<TCommand, TResponse>();
        var pipeline = integrator.BuildCommandPipeline(builder);

        return WithCommand<TCommand, TResponse, THandler>(pipeline);
    }

    /// <summary>
    /// Registers a command handler for a specific command type and response type.
    /// </summary>
    /// <param name="pipelineBuilder">Pipeline builder with added pipeline types.</param>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResponse">The type of the command response.</typeparam>
    /// <typeparam name="THandler">The type of the command handler.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithCommand<TCommand, TResponse, THandler>(ServicePipelineBuilder<TCommand, TResponse> pipelineBuilder)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        WithCommand<TCommand, TResponse, THandler>();
        services.AddScoped(pipelineBuilder.Build);
        return this;
    }

    /// <inheritdoc cref="WithCommand{TCommand, THandler}(ServicePipelineBuilder{TCommand})"/>/>
    public ServiceMediatorConfig WithCommand<TCommand, THandler>()
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>, THandler>();
        return this;
    }

    /// <inheritdoc cref="WithCommand{TCommand, THandler}(ServicePipelineBuilder{TCommand})"/>/>
    public ServiceMediatorConfig WithCommand<TCommand, THandler>(IServicePipelineIntegrator integrator)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        integrator.RegisterUnitCommandPipeline<TCommand>(services);

        var builder = new ServicePipelineBuilder<TCommand>();
        var pipeline = integrator.BuildUnitCommandPipeline(builder);

        return WithCommand<TCommand, THandler>(pipeline);
    }

    /// <summary>
    /// Registers a command handler for a specific command type and response type.
    /// </summary>
    /// <param name="pipelineBuilder">Pipeline builder with added pipeline types.</param>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="THandler">The type of the command handler.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithCommand<TCommand, THandler>(ServicePipelineBuilder<TCommand> pipelineBuilder)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        WithCommand<TCommand, THandler>();
        services.AddScoped(pipelineBuilder.Build);
        return this;
    }
}
