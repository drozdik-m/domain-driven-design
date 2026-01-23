using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.OpenApi;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <inheritdoc cref="AddAppOpenApi(IServiceCollection, Action{OpenApiOptions})"/>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection serviceCollection)
    {
        return AddAppOpenApi(serviceCollection, _ => { });
    }

    /// <summary>
    /// Adds generally accepted application-specific OpenAPI configuration.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to extend.</param>
    /// <param name="configureOptions">Action to configure <see cref="OpenApiOptions"/>.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection serviceCollection, Action<OpenApiOptions> configureOptions)
    {
        return serviceCollection.AddOpenApi(options =>
        {
            options.ParentDeclarationSchemaIds();
            configureOptions(options);
        });
    }
}
