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
        WithCommand<TCommand, THandler>();
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
        services.AddScoped<ICommandHandler<TCommand>, THandler>();
        services.AddScoped(pipelineBuilder.Build);
        return this;
    }

    /// <summary>
    /// Searches for all request handlers in the specified assembly and registers them.
    /// </summary>
    /// <typeparam name="TAssembly">Type of the assembly to go throught.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithRequestsFromAssembly<TAssembly>()
    {
        // Get all non-abstract types in the assembly
        var nonAbstractTypes = GetAssemblyNonAbstractTypes<TAssembly>();

        // Register all handlers
        RegisterHandlerTypes(nonAbstractTypes, typeof(IQueryHandler<,>));
        RegisterHandlerTypes(nonAbstractTypes, typeof(ICommandHandler<,>));
        RegisterHandlerTypes(nonAbstractTypes, typeof(ICommandHandler<>));

        return this;

        void RegisterHandlerTypes(IEnumerable<Type> nonAbstractTypes, Type handlerType)
        {
            foreach (var item in GetTypeWithSearchedInterface(nonAbstractTypes, handlerType))
            {
                services.AddScoped(item.Interface, item.ActualType);
            }
        }
    }

    /// <summary>
    /// Searches for all pipeline behaviours in the specified assembly and registers them for all commands and queries.
    /// </summary>
    /// <typeparam name="TAssembly">Type of the assembly to go throught.</typeparam>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithPipelinesFromAssembly<TAssembly>()
    {
        // Get all non-abstract types in the assembly
        var nonAbstractTypes = GetAssemblyNonAbstractTypes<TAssembly>();

        // Register all handlers
        RegistePipelineTypes(nonAbstractTypes, typeof(IQueryHandler<,>));
        RegistePipelineTypes(nonAbstractTypes, typeof(ICommandHandler<,>));
        RegistePipelineTypes(nonAbstractTypes, typeof(ICommandHandler<>));

        return this;

        void RegistePipelineTypes(IEnumerable<Type> nonAbstractTypes, Type handlerType)
        {
            foreach (var item in GetTypeWithSearchedInterface(nonAbstractTypes, handlerType))
            {
                var pipelineBuilderType = typeof(ServicePipelineBuilder<,>).MakeGenericType(item.Interface.GetGenericArguments());
                // TODO finish
                //services.AddScoped(item.Interface, item.ActualType);
            }

            /*var pipelineBuilderType = typeof(ServicePipelineBuilder<,>).MakeGenericType(item.Interface.GetGenericArguments());
                services.AddScoped(pipelineBuilderType, sp => Activator.CreateInstance(pipelineBuilderType));
                services.AddScoped(item.Interface, item.Handler);*/
        }
    }

    /// <summary>
    /// Returns all non-abstract types from the specified assembly.
    /// </summary>
    /// <typeparam name="TAssembly">Type of the assembly</typeparam>
    /// <returns>All non abstract types from the assembly.</returns>
    private static Type[] GetAssemblyNonAbstractTypes<TAssembly>()
    {
        // Get the assembly of the specified type
        var assembly = typeof(TAssembly).Assembly;

        // Get all non-abstract types in the assembly
        var nonAbstractTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToArray();

        return nonAbstractTypes;
    }

    /// <summary>
    /// Returns all types that implement the searched interface from the specified non-abstract type list.
    /// </summary>
    /// <param name="typeList">List of types to check.</param>
    /// <param name="searchedInterface">The interface to look for.</param>
    private static ClassWithInterface[] GetTypeWithSearchedInterface(IEnumerable<Type> typeList, Type searchedInterface)
    {
        var numberOfGenericArguments = searchedInterface.GetGenericArguments().Length;
        var typesWithInterfaces = typeList
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == searchedInterface && i.GetGenericArguments().Length == numberOfGenericArguments)
                        .Select(i => new ClassWithInterface(t, i)))
                    .ToArray();
        return typesWithInterfaces;
    }

    private record struct ClassWithInterface(Type ActualType, Type Interface);
}
