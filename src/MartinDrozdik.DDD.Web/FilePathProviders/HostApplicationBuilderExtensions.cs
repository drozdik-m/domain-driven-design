using MartinDrozdik.DDD.Web.FilePathProviders.Static;
using MartinDrozdik.DDD.Web.FilePathProviders.StaticResources;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.FilePathProviders;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="IStaticFilePathProvider"/>.
    /// Uses <see cref="TimestampedStaticFilePathProvider"/> in development environment to eliminate caching problems during development.
    /// Uses and <see cref="VersionedStaticFilePathProvider"/> in production environment for correct resource caching and busting.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    public static IHostApplicationBuilder AddStaticFilePathProvider(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatedAppOptions<StaticFileVersioningOptions>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IStaticFilePathProvider, TimestampedStaticFilePathProvider>();
        }
        else
        {
            builder.Services.AddSingleton<IStaticFilePathProvider, VersionedStaticFilePathProvider>();
        }

        return builder;
    }

    /// <summary>
    /// Adds <see cref="IStaticFilePathProvider"/> that does nothing to the inserted path.
    /// Useful when you want to disable static file versioning, f.e. when using CDN with its own versioning or in development environment where caching is not a problem.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    public static IHostApplicationBuilder AddIdentityStaticFilePathProvider(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IStaticFilePathProvider, IdentityStaticFilePathProvider>();
        return builder;
    }
}
