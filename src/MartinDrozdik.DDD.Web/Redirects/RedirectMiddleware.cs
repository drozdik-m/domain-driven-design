using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// Answers a request whose path has a <see cref="Redirect"/> with redirect status code and <c>Location</c>.
/// Passes any other request on.
/// </summary>
/// <param name="next">The next middleware.</param>
/// <param name="registry">The redirects of the application.</param>
/// <param name="logger">The logger.</param>
internal sealed class RedirectMiddleware(RequestDelegate next, RedirectRegistry registry, ILogger<RedirectMiddleware> logger)
{
    /// <summary>
    /// Redirects the request when its path has a <see cref="Redirect"/>, else calls the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the request is handled.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!registry.TryGet(context.Request.Path, out var registration))
        {
            return next(context);
        }

        var location = registration.Redirect.BuildLocationHeader(context.Request);
        context.Response.StatusCode = registration.StatusCode;
        context.Response.Headers[HeaderNames.Location] = location;

        if (logger.IsEnabled(LogLevel.Information))
        {
            RedirectLogging.LogRedirected(logger, context.Request.Method, context.Request.Path.Value ?? string.Empty, location, registration.StatusCode);
        }

        return Task.CompletedTask;
    }
}
