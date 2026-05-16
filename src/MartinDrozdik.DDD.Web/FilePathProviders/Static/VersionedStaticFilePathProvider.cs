using MartinDrozdik.DDD.Integrations;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.FilePathProviders.Static;

/// <summary>
/// Uses predefined version to update the path with a query parameter, which can be used for cache busting.
/// The version is provided through options, so it can be easily updated without changing the codebase, for example during deployment or as part of a build process.
/// </summary>
/// <param name="options">The options containing versioning information.</param>
public class VersionedStaticFilePathProvider(IOptions<StaticFileVersioningOptions> options) : IStaticFilePathProvider
{
    /// <inheritdoc />
    public string PathTo(string path)
    {
        return UrlBuilder.FromUrl(path)
            .WithQueryParameter("version", options.Value.Version.ToString())
            .Build();
    }
}
