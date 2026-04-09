namespace MartinDrozdik.DDD.Web.FilePathProviders.Static;

/// <summary>
/// Does no modifications to the original path.
/// </summary>
public class IdentityStaticFilePathProvider : IStaticFilePathProvider
{
    /// <inheritdoc/>
    public string PathTo(string path) => path;
}
