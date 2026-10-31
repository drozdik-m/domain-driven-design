using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// Extensions for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Answers requests to the old paths added by
    /// <see cref="HostApplicationBuilderExtensions.AddPermanentRedirect{TBuilder}(TBuilder, Redirect)"/> and
    /// <see cref="HostApplicationBuilderExtensions.AddTemporaryRedirect{TBuilder}(TBuilder, Redirect)"/>.
    /// A redirect answers before the middlewares added after this call, e.g. call it before
    /// <see cref="StaticFileExtensions.UseStaticFiles(IApplicationBuilder)"/> to redirect an old file that still exists.
    /// Does nothing when the application has no redirects.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseRedirects(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.ApplicationServices.GetService<RedirectRegistry>() is not null)
        {
            app.UseMiddleware<RedirectMiddleware>();
        }

        return app;
    }
}
