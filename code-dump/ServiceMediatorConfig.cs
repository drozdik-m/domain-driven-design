using System.Reflection;
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

    /// <summary>
    /// Searches for all query handlers in the specified assembly and registers them along with provided pipelines.
    /// </summary>
    /// <typeparam name="TAssembly">Type of the assembly to go throught.</typeparam>
    /// <param name="pipelineTypes">Pipeline types to register for each query handler.</param>
    /// <returns>This for chaining.</returns>
    public ServiceMediatorConfig WithQueriesFromAssembly<TAssembly>(params IEnumerable<Type> pipelineTypes)
    {
        // Assert that all provided types...
        foreach (var pipelineType in pipelineTypes)
        {
            ArgumentNullException.ThrowIfNull(pipelineType);

            var pipelineBehaviourInterface = pipelineType
                .GetInterfaces()
                .SingleOrDefault(e => e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            // ...that they implement IPipelineBehavior<,>
            if (pipelineBehaviourInterface is null)
            {
                throw new ArgumentException($"Type {pipelineType.Name} does not implement {typeof(IPipelineBehavior<,>).Name}", nameof(pipelineTypes));
            }

            // ...and that they have suitable generic arguments
            var genericArguments = pipelineBehaviourInterface.GetGenericArguments();
            if (genericArguments.Length != 2)
            {
                throw new ArgumentException($"Type {pipelineType.Name} does not implement {typeof(IPipelineBehavior<,>).Name} with two generic arguments", nameof(pipelineTypes));
            }
        }

        // Get all non-abstract types in the assembly
        var nonAbstractTypes = GetAssemblyNonAbstractClassTypes<TAssembly>();

        // Register all queries
        RegisterHandlerTypes(nonAbstractTypes, typeof(IQueryHandler<,>));

        return this;

        void RegisterHandlerTypes(IEnumerable<Type> nonAbstractTypes, Type handlerType)
        {
            // Find all types that implement the query interface
            foreach (var item in GetTypesWithInterface(nonAbstractTypes, handlerType))
            {
                // Register the handler
                services.AddScoped(item.Interface, item.ActualType);

                // Register the pipeline for the handler
                RegisterTwoArgServicePipelineBuilder(services, item, pipelineTypes);
            }
        }
    }

    /// <summary>
    /// Registers all request handlers with a return value in the specified assembly along with provided pipelines.
    /// </summary>
    private static void RegisterTwoArgServicePipelineBuilder(IServiceCollection services, ClassWithInterface handlerInfo, IEnumerable<Type> pipelineTypes)
    {
        RegisterServicePipelineBuilder(
            typeof(ServicePipelineBuilder<,>),
            typeof(IPipelineBehavior<,>),
            services,
            handlerInfo,
            pipelineTypes);
    }

    /// <summary>
    /// Registers all request handlers without return value in the specified assembly along with provided pipelines.
    /// </summary>
    private static void RegisterOneArgServicePipelineBuilder(IServiceCollection services, ClassWithInterface handlerInfo, IEnumerable<Type> pipelineTypes)
    {
        RegisterServicePipelineBuilder(
            typeof(ServicePipelineBuilder<>),
            typeof(IPipelineBehavior<>),
            services,
            handlerInfo,
            pipelineTypes);
    }

    /// <summary>
    /// Registers all request handlers in the specified assembly along with provided pipelines.
    /// </summary>
    private static void RegisterServicePipelineBuilder(
        Type actualServicePipelineBuilderType,
        Type interfaceServicePipelineBuilderType,
        IServiceCollection services,
        ClassWithInterface handlerInfo,
        IEnumerable<Type> pipelineTypes)
    {
        // If there are pipeline types provided, register the pipelines for the specific request/response types
        // Get the actual builder type and instance
        var genericArguments = handlerInfo.Interface.GetGenericArguments(); // Leverage that the generic arguments are the same
        var pipelineBuilderType = actualServicePipelineBuilderType.MakeGenericType(genericArguments);
        var pipelineBuilderInstance = Activator.CreateInstance(pipelineBuilderType)
            ?? throw new InvalidOperationException("Could not create instance of the pipeline builder.");

        GetPipelineBuilderMethods(pipelineBuilderType, out var addMethod, out var buildMethod);

        // Add all provided pipeline types to the builder
        foreach (var pipelineType in pipelineTypes)
        {
            addMethod.Invoke(pipelineBuilderInstance, [pipelineType]);
        }

        var pipelineInterfaceType = interfaceServicePipelineBuilderType.MakeGenericType(genericArguments);
        services.AddScoped(
               pipelineInterfaceType,
               provider => buildMethod.Invoke(pipelineBuilderInstance, [provider]) ?? throw new InvalidOperationException("Could execute 'Build' method on the pipeline builder."));
    }

    /// <summary>
    /// Finds <see cref="MethodInfo"/> the 'Add' and 'Build' methods of the pipeline builder type.
    /// </summary>
    /// <param name="pipelineBuilderType">The type of the pipeline.</param>
    /// <param name="addMethod">Add method on the pipeline.</param>
    /// <param name="buildMethod">Build method on the pipeline.</param>
    private static void GetPipelineBuilderMethods(Type pipelineBuilderType, out MethodInfo addMethod, out MethodInfo buildMethod)
    {
        // Get the methods
        addMethod = pipelineBuilderType.GetMethod("Add", [typeof(Type)])
                ?? throw new InvalidOperationException("Could find the 'Add' method on the pipeline builder.");
        buildMethod = pipelineBuilderType.GetMethod("Build", [typeof(IServiceProvider)])
                ?? throw new InvalidOperationException("Could find the 'Build' method on the pipeline builder.");
    }

    /// <summary>
    /// Returns all non-abstract class types from the specified assembly.
    /// </summary>
    /// <typeparam name="TAssembly">Type of the assembly.</typeparam>
    /// <returns>All non abstract types from the assembly.</returns>
    private static Type[] GetAssemblyNonAbstractClassTypes<TAssembly>()
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
    private static ClassWithInterface[] GetTypesWithInterface(IEnumerable<Type> typeList, Type searchedInterface)
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
