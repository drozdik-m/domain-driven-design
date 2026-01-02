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
    /// <summary>
    /// Defines methods for constructing service pipelines to process queries and commands within an application.
    /// </summary>
    public interface IPipelineAssistant
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
        /// <typeparam name="TQuery">Type of the query.</typeparam>
        /// <typeparam name="TOutput">Type of the output.</typeparam>
        /// <returns>The pipeline.</returns>
        public abstract ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>()
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
        /// <typeparam name="TCommand">Type of the command.</typeparam>
        /// <typeparam name="TOutput">Type of the output.</typeparam>
        /// <returns>The pipeline.</returns>
        public abstract ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>()
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
        /// <typeparam name="TCommand">Type of the command.</typeparam>
        /// <returns>The pipeline.</returns>
        public abstract ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>()
            where TCommand : ICommand;
    }

    /// <inheritdoc cref="WithQuery{TQuery, TResponse, THandler}(ServicePipelineBuilder{TQuery, TResponse})"/>
    public ServiceMediatorConfig WithQuery<TQuery, TResponse, THandler>()
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<IQueryHandler<TQuery, TResponse>, THandler>();
        return this;
    }

    /// <inheritdoc cref="WithQuery{TQuery, TResponse, THandler}(ServicePipelineBuilder{TQuery, TResponse})"/>
    public ServiceMediatorConfig WithQuery<TQuery, TResponse, THandler>(IPipelineAssistant buildPipeline)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        buildPipeline.RegisterQueryPipeline<TQuery, TResponse>(services);
        var pipelineBuilder = buildPipeline.BuildQueryPipeline<TQuery, TResponse>();
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
    public ServiceMediatorConfig WithCommand<TCommand, TResponse, THandler>(IPipelineAssistant buildPipeline)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        buildPipeline.RegisterCommandPipeline<TCommand, TResponse>(services);
        var pipeline = buildPipeline.BuildCommandPipeline<TCommand, TResponse>();
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
    public ServiceMediatorConfig WithCommand<TCommand, THandler>(IPipelineAssistant buildPipeline)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        buildPipeline.RegisterUnitCommandPipeline<TCommand>(services);
        var pipeline = buildPipeline.BuildUnitCommandPipeline<TCommand>();
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
