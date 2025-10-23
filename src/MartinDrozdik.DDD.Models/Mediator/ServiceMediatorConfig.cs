using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Mediator;

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
