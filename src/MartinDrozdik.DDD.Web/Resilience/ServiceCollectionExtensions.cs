using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Resilience;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds resilience handlers to HTTP clients by default.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to extend.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHttpClientResilience(this IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });
        return serviceCollection;
    }
}
