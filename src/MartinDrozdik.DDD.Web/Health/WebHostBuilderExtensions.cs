using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MartinDrozdik.DDD.Web.Health;

/// <summary>
/// Extensions for <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Adds default health check configuration with request timeouts and output caching.
    /// Configures a basic liveness probe to verify the application is responsive.
    /// </summary>
    /// <remarks>
    /// Also add further dependancy health checks via <paramref name="healthCheckBuilderConfig"/>:
    /// - Example: .AddDbContextCheck{AppDbContext}(tags: ["ready"])
    /// - Example: .AddRedis("redis-connection-string", tags: ["ready"])
    /// ...etc.
    /// </remarks>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="healthCheckBuilderConfig">Optional action to further configure health checks.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder AddAppHealthChecks(this WebApplicationBuilder builder, Action<IHealthChecksBuilder>? healthCheckBuilderConfig = null)
    {
        // Timeout policy prevents health check endpoints from hanging indefinitely
        // 10 seconds is reasonable for most health checks
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        // Basic liveness check - just confirms the app process is running and can respond
        // This is intentionally simple - it should NOT check dependencies
        // Tagged with "live" for use with /health/live endpoint
        var healthCheckBuilder = builder.Services.AddHealthChecks()

            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // Allow further configuration of health checks
        healthCheckBuilderConfig?.Invoke(healthCheckBuilder);

        return builder;
    }
}
