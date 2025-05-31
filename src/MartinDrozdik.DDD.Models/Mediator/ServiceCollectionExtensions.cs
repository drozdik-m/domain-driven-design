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
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        //services.AddSingleton<IMediator, ServiceMediator>();
        return services;
    }
}
