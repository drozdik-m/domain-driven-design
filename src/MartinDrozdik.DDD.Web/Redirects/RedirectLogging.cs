using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// Source-generated log messages emitted by <see cref="RedirectMiddleware"/>.
/// </summary>
internal static partial class RedirectLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Redirected {Method} {Path} to {Location} with status code {StatusCode}.")]
    internal static partial void LogRedirected(ILogger logger, string method, string path, string location, int statusCode);
}
