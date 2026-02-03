using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace MartinDrozdik.DDD.Web.Proxy;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Uses forwarded headers to correctly handle client IP and protocol when behind a reverse proxy (like nginx or YARP).
    /// Make sure to configure your reverse proxy to forward these headers and to trust the proxy's IP address(es) if necessary.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to extend.</param>
    /// <returns>The <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication IsBehindProxy(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All,
        });

        return app;
    }
}
