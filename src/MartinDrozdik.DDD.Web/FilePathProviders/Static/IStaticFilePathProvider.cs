namespace MartinDrozdik.DDD.Web.FilePathProviders.Static;

/// <summary>
/// The intefrace provides abstraction for creating paths to static files, usually from wwwroot.
/// Also provides method overloads for RazorClassLibrary resources.
/// The main purpose is to provide a single point of configuration for static resource paths, which can be useful for versioning, CDN integration, or other path modifications without changing the codebase.
/// </summary>
public interface IStaticFilePathProvider
{
    /// <summary>
    /// Creates modified path to a resource.
    /// </summary>
    /// <example>/Fonts/OpenSans.svg -> /Fonts/OpenSans.svg?version=1.0.0.</example>
    /// <param name="path">Target absolute path without domain, f.e. (/Fonts/OpenSans.svg).</param>
    /// <returns>Updated path.</returns>
    string PathTo(string path);
}
