using Microsoft.AspNetCore.Builder;

namespace MartinDrozdik.DDD.Web.Health;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps default health check endpoints for liveness and readiness probes.
    /// </summary>
    /// <remarks>
    /// <para><strong>Liveness endpoint (/health/live):</strong> Checks if the app process is responsive. Only includes checks tagged with "live". If this fails, the orchestrator should restart the app.</para>
    /// <para><strong>Readiness endpoint (/health/ready):</strong> Checks if the app is ready to serve traffic. Includes checks tagged with "ready". If this fails, the orchestrator should remove the app from load balancing but NOT restart it.</para>
    /// <para><strong>General health endpoint (/health):</strong> Runs ALL health checks. Useful for overall status monitoring and dashboards.</para>
    /// </remarks>
    /// <param name="app">The <see cref="WebApplication"/> to extend.</param>
    /// <returns>The <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        var healthChecks = app.MapGroup(string.Empty);
        healthChecks
            .WithRequestTimeout("HealthChecks");

        // Liveness probe: Is the app alive? (restart if fails)
        // Only checks tagged with "live" - should be simple and not check dependencies
        healthChecks.MapHealthChecks(
            "/health/live",
            new() { Predicate = static r => r.Tags.Contains("live"), });

        // Readiness probe: Is the app ready to serve traffic? (remove from load balancer if fails)
        // Checks tagged with "ready" - includes dependency checks (DB, cache, APIs, etc.)
        healthChecks.MapHealthChecks(
            "/health/ready",
            new() { Predicate = static r => r.Tags.Contains("ready"), });

        // General health endpoint: All health checks (both "live" and "ready")
        // Useful for monitoring dashboards and overall application health status
        healthChecks.MapHealthChecks("/health");

        return app;
    }
}
