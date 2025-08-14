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
    /// <param name="config">Configuration action for the mediator.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<ServiceMediatorConfig> config)
    {
        // Add the Mediator service
        services.AddSingleton<IMediator, ServiceMediator>();

        // Add handlers for requests
        var mediatorBuilder = new ServiceMediatorConfig(services);
        config(mediatorBuilder);

        return services;
    }
}
