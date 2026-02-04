using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Logging;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Sets up basic loggin to console and debug (in development).
    /// Logs HTTP requests in development.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="minimumLogLevel">Minimum log level to log. <see cref="LogLevel.Information"/> by default. Consider less logging on higher environments.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    public static IHostApplicationBuilder AddAppLogging(this IHostApplicationBuilder builder, LogLevel minimumLogLevel = LogLevel.Information)
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
