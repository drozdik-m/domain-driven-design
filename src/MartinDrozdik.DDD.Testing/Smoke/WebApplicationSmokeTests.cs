using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Base class for smoke tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
/// <param name="factoryBuilder">App builder under test.</param>
public abstract class WebApplicationSmokeTests<TProgram>(TestedAppBuilder<TProgram> factoryBuilder)
    where TProgram : class
{
    /// <summary>
    /// All routes that must return non-5xx responses.
    /// </summary>
    /// <remarks>
    /// Override to add app-specific endpoints.
    /// </remarks>
    /// <returns>Health endpoints.</returns>
    public static TheoryData<string> GetHealthEndpoints() =>
    [
        "/health",
        "/health/live",
        "/health/ready",
    ];

    /// <summary>
    /// Smoke test to verify that the app starts without throwing an exception and responds to requests.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task App_starts_without_exception()
    {
        // Arrange
        using var factory = factoryBuilder.Build();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Smoke test to verify that the health endpoints return healthy status.
    /// </summary>
    /// <param name="endpoint">Target endpoint URL.</param>
    /// <returns><see cref="Task"/>.</returns>
    [Theory]
    [MemberData(nameof(GetHealthEndpoints))]
    public async Task Health_endpoint_returns_healthy(string endpoint)
    {
        // Arrange
        using var factory = factoryBuilder.Build();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Unhealthy", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Degraded", body, StringComparison.OrdinalIgnoreCase);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Healthy", content);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.CacheControl);
    }

    /// <summary>
    /// Verifies that the response headers from endpoints do not expose sensitive information.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Response_headers_are_correct()
    {
        // Arrange
        using var factory = factoryBuilder.Build();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        // Don't leak info
        AssertSensityHeaderNotPresent("Server");
        AssertSensityHeaderNotPresent("X-AspNet-Version");
        AssertSensityHeaderNotPresent("X-Powered-By");
        AssertSensityHeaderNotPresent("X-AspNetMvc-Version");
        AssertSensityHeaderNotPresent("X-Response-Time");
        AssertSensityHeaderNotPresent("X-Runtime");
        AssertSensityHeaderNotPresent("X-Debug-Token");
        AssertSensityHeaderNotPresent("X-Debug-Token-Link");

        return;
        void AssertSensityHeaderNotPresent(string headerName)
            => Assert.False(response.Headers.Contains(headerName), $"The header \"{headerName}\" may leak vulnerable information.");
    }

    /// <summary>
    /// Smoke test to verify that all options configured with ValidateOnStart() are valid.
    /// </summary>
    [Fact]
    public void All_options_are_valid()
    {
        // Arrange
        using var factory = factoryBuilder.Build();
        var validator = factory.Services.GetRequiredService<IStartupValidator>();

        // Act & Assert
        // Runs all IValidateOptions<T> registrations that used ValidateOnStart().
        // Throws OptionsValidationException with all failures if anything is invalid.
        validator.Validate();
    }

    /// <summary>
    /// Smoke test to verify that services are configured correctly.
    /// </summary>
    [Fact]
    public void All_services_are_valid()
    {
        // Arrange
        using var factory = factoryBuilder
            .With(builder =>
            {
                builder.UseDefaultServiceProvider(options =>
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                });
            })
            .Build();

        // Act & Assert
        factory.StartServer();
    }
}
