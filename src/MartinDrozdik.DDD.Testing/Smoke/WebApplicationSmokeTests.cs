using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Base class for smoke tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TFactoryBuilder">Type of the app factory.</typeparam>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
public abstract class WebApplicationSmokeTests<TFactoryBuilder, TProgram> : IDisposable
    where TFactoryBuilder : TestWebApplicationFactoryBuilder<TProgram>
    where TProgram : class
{
    private readonly TestWebApplicationFactory<TProgram> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationSmokeTests{TWebApp, TProgram}"/> class.
    /// </summary>
    /// <param name="factoryBuilder">App builder under test.</param>
    protected WebApplicationSmokeTests(TFactoryBuilder factoryBuilder)
    {
        _factory = factoryBuilder.Build();
    }

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
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

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
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Unhealthy", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Degraded", body, StringComparison.OrdinalIgnoreCase);

        var content = await response.Content.ReadAsStringAsync();
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
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

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
        var validator = _factory.Services.GetRequiredService<IStartupValidator>();

        // Runs all IValidateOptions<T> registrations that used ValidateOnStart().
        // Throws OptionsValidationException with all failures if anything is invalid.
        validator.Validate();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }
    }
}
