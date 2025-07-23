using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Mediator;

/// <summary>
/// Extension methods for adding Mediator services to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mediator services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<ServiceMediatorBuilder> builder)
    {
        // Add the Mediator service
        services.AddSingleton<IMediator, ServiceMediator>();

        // Add handlers for requests
        var mediatorBuilder = new ServiceMediatorBuilder(services);
        builder(mediatorBuilder);

        return services;
    }
}

public class ServiceMediatorBuilder(IServiceCollection services)
{
    public ServiceMediatorBuilder WithQuery<TQuery, TResponse, THandler>()
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<IQueryHandler<TQuery, TResponse>, THandler>();
        return this;
    }

    public ServiceMediatorBuilder WithCommand<TCommand, TResponse, THandler>()
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<ICommandHandler<TCommand, TResponse>, THandler>();
        return this;
    }

    public ServiceMediatorBuilder WithCommand<TCommand, THandler>()
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>, THandler>();
        return this;
    }

    public ServiceMediatorBuilder WithRequestsFromAssembly<TAssembly>()
    {
        // Get the assembly of the specified type
        var assembly = typeof(TAssembly).Assembly;

        var nonAbstractTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        // Register all query handlers
        var queryHandlers = nonAbstractTypes
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(i => new { Handler = t, Interface = i }))
            .ToList();

        foreach (var item in queryHandlers)
        {
            services.AddScoped(item.Interface, item.Handler);
        }

        // Register all command handlers with TResponse
        var commandHandlers = nonAbstractTypes
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .Select(i => new { Handler = t, Interface = i }))
            .ToList();

        foreach (var item in commandHandlers)
        {
            services.AddScoped(item.Interface, item.Handler);
        }

        // Register all command handlers without TResponse
        var unitCommandHandlers = nonAbstractTypes
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                    && i.GetGenericArguments().Length == 1)
                .Select(i => new { Handler = t, Interface = i }))
            .ToList();

        foreach (var item in unitCommandHandlers)
        {
            services.AddScoped(item.Interface, item.Handler);
        }

        return this;
    }
}
