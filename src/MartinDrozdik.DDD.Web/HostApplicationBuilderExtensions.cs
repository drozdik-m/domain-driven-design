using MartinDrozdik.DDD.Web.FilePathProviders;
using MartinDrozdik.DDD.Web.Health;
using MartinDrozdik.DDD.Web.Logging;
using MartinDrozdik.DDD.Web.Middlewares;
using MartinDrozdik.DDD.Web.OpenApi;
using MartinDrozdik.DDD.Web.Resilience;
using MartinDrozdik.DDD.Web.Telemetry;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <inheritdoc cref="AddAppServices{TBuilder}(TBuilder, WebApplicationOptions)" />
    public static TBuilder AddAppServices<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddAppServices(WebApplicationOptions.Default);
    }

    /// <summary>
    /// Adds library-defined:
    /// <list type="bullet">
    ///     <item>Logging</item>
    ///     <item>Error Handling Middlewares</item>
    ///     <item>OpenAPI</item>
    ///     <item>Health Checks</item>
    ///     <item>OpenTelemetry</item>
    ///     <item>HTTP Client Resilience</item>
    ///     <item>Cached static file path provider</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TBuilder">Type of <see cref="IHostApplicationBuilder"/> to configure.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="options">Options for configuring the application. Turn on/off features etc.</param>
    /// <returns><typeparamref name="TBuilder"/> for chaining.</returns>
    public static TBuilder AddAppServices<TBuilder>(this TBuilder builder, WebApplicationOptions options)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddAppLogging();
        builder.Services.AddAppErrorHandling();
        builder.Services.AddAppOpenApi();
        builder.AddAppHealthChecks();
        if (options.UseOpenTelemetry)
        {
            builder.AddAppOpenTelemetry();
        }

        builder.Services.AddHttpClientResilience();

        if (options.UseStaticFilePathProvider)
        {
            builder.AddStaticFilePathProvider();
        }

        return builder;
    }
}
