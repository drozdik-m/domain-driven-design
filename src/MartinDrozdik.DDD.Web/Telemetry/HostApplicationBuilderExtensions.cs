using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MartinDrozdik.DDD.Web.Telemetry;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Configures OpenTelemetry with logging, metrics, and tracing instrumentation.
    /// Call this method to enable observability in your application.
    /// </summary>
    /// <typeparam name="TBuilder">Type of <see cref="IHostApplicationBuilder"/> to configure.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns><typeparamref name="TBuilder"/> for chaining.</returns>
    public static TBuilder AddAppOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Configure OpenTelemetry logging integration
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(
                        serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? builder.Environment.ApplicationName,
                        serviceVersion: builder.Configuration["OTEL_SERVICE_VERSION"] ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName,
                        ["host.name"] = Environment.MachineName,
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                    {
                        // Exclude health check requests from tracing
                        tracing.Filter = context => !context.Request.Path.StartsWithSegments(Health.WebApplicationExtensions.HealthPathPrefix);
                    })
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry exporters based on the application configuration.
    /// Currently supports OTLP (OpenTelemetry Protocol) exporter when endpoint is configured.
    /// </summary>
    /// <typeparam name="TBuilder">Type of <see cref="IHostApplicationBuilder"/> to configure.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns><typeparamref name="TBuilder"/> for chaining.</returns>
    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Check if OTLP exporter endpoint is configured via environment variable or appsettings
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var useOtlpExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

        if (useOtlpExporter)
        {
            // Register OTLP exporter for traces, metrics, and logs
            builder.Services
                .AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }
}
