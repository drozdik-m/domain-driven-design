using FluentValidation;
using MartinDrozdik.DDD.Demo.Middlewares.Exceptions;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web.Middlewares.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Logging;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Sets up basic loggin to console and debug (in development).
    /// Logs HTTP requests in development.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to extend.</param>
    /// <returns>Updated <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder AddAppLogging(this WebApplicationBuilder builder, LogLevel minimumLogLevel = LogLevel.Information)
    {
        builder.Logging.AddConsole();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.All;
                options.CombineLogs = true;
            });
        }

        builder.Logging.SetMinimumLevel(minimumLogLevel);
        return builder;
    }
}
