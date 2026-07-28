using MartinDrozdik.DDD.Web.Environments;
using MartinDrozdik.DDD.Web.Health;
using MartinDrozdik.DDD.Web.Middlewares.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <inheritdoc cref="UseAppMiddlewares(WebApplication, WebApplicationOptions)" />
    public static WebApplication UseAppMiddlewares(this WebApplication app)
    {
        return UseAppMiddlewares(app, WebApplicationOptions.Default);
    }

    /// <summary>
    /// Adds library-defined:
    /// <list type="bullet">
    ///     <item>Request/Response Logging Middleware</item>
    ///     <item>Exception Handler Middleware</item>
    ///     <item>Health Check Endpoints</item>
    /// </list>
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to extend.</param>
    /// <param name="options">Options for configuring the application. Turn on/off features etc.</param>
    /// <returns>The <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication UseAppMiddlewares(this WebApplication app, WebApplicationOptions options)
    {
        // Log startup info
        var logger = app.Services.GetService<ILogger>();
        if (logger is not null && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("App working with environment {Environment}", app.Environment.EnvironmentName);
        }

        // Add middlewares
        // * The logging middleware is registered first so that it logs the most final results
        // * Should be definitely before exception handlers
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        app.UseExceptionHandler();
        app.MapAppHealthChecks();
        if (app.Environment.IsDevelopment() || app.Environment.IsTesting())
        {
            app.UseHttpLogging();
        }

        return app;
    }
}
