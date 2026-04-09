using MartinDrozdik.DDD.Integrations;

namespace MartinDrozdik.DDD.Web.FilePathProviders.Static;

/// <summary>
/// Adds timestamp as a query parameter to the path, which can be used for cache busting. The timestamp is generated at the moment of calling the method, so it will be different for each call, ensuring that the browser fetches the latest version of the resource.
/// </summary>
public class TimestampedStaticFilePathProvider(TimeProvider timeProvider) : IStaticFilePathProvider
{
    /// <inheritdoc />
    public string PathTo(string path)
    {
        var time = timeProvider.GetUtcNow();
        var timestamp = time.ToUnixTimeMilliseconds();
        return UrlBuilder.FromUrl(path)
            .WithQueryParameter("version", timestamp.ToString())
            .Build();
    }
}
