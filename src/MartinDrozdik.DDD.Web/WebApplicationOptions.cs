using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web;

/// <summary>
/// Options for <see cref="HostApplicationBuilderExtensions.AddAppServices{TBuilder}(TBuilder, WebApplicationOptions)"/>.
/// Sometimes you just don't need everything.
/// Turn off unwanted features.
/// </summary>
public record WebApplicationOptions
{
    /// <summary>
    /// Gets the default options with all features turned on.
    /// </summary>
    public static WebApplicationOptions Default { get; } = new()
    {
        UseOpenTelemetry = true,
        UseStaticFilePathProvider = true,
    };

    /// <summary>
    /// Gets a value indicating whether to use <see cref="FilePathProviders.HostApplicationBuilderExtensions.AddStaticFilePathProvider(IHostApplicationBuilder)"/>.
    /// </summary>
    public bool UseStaticFilePathProvider { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to use <see cref="Telemetry.HostApplicationBuilderExtensions.AddAppOpenTelemetry{TBuilder}(TBuilder)"/>.
    /// </summary>
    public bool UseOpenTelemetry { get; init; } = true;
}
