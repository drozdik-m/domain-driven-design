using MartinDrozdik.DDD.Web.Health;
using MartinDrozdik.DDD.Web.Middlewares.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Adds library-defined:
    /// <list type="bullet">
    ///     <item>Exception Handler Middleware</item>
    ///     <item>Health Check Endpoints</item>
    ///     <item>Request/Response Logging Middleware</item>
    /// </list>
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to extend.</param>
    /// <returns>The <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication UseAppMiddlewares(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.MapAppHealthChecks();
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.UseHttpLogging();
        }

        return app;
    }
}
