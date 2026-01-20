using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.OpenApi;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds generally accepted application-specific OpenAPI configuration.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to extend.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddOpenApi(options => options.ParentDeclarationSchemaIds());
    }
}
