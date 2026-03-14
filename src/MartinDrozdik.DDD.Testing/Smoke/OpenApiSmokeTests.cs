using System.Text.Json;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Base class for smoke tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TWebApp">Type of the app factory.</typeparam>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
public abstract class OpenApiSmokeTests<TWebApp, TProgram> : IDisposable
    where TWebApp : TestWebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly TWebApp _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSmokeTests{TWebApp, TProgram}"/> class.
    /// </summary>
    /// <param name="factory">App factory under test. Disposed automatically.</param>
    protected OpenApiSmokeTests(TWebApp factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Expected format of the OpenAPI document served by the app.
    /// </summary>
    public enum OpenApiType
    {
        /// <summary>
        /// The OpenAPI document is served as JSON, typically at an endpoint like /openapi/doc.json.
        /// </summary>
        Json,

        /// <summary>
        /// The OpenAPI document is served as YAML, typically at an endpoint like /openapi/doc.yaml.
        /// </summary>
        Yaml,
    }

    /// <summary>
    /// Represents an OpenAPI endpoint to be tested.
    /// </summary>
    /// <param name="Url">Target URL.</param>
    /// <param name="Type">Document type.</param>
    public record OpenApiEndpoint(string Url, OpenApiType Type);

    /// <summary>
    /// Smoke test to verify that the OpenAPI document.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task OpenApi_endpoints_return_a_document()
    {
        foreach (var openApi in GetOpenApiEndpoints())
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(openApi.Url);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            if (openApi.Type == OpenApiType.Json)
            {
                ValidateJsonOpenApiDocument(content);
            }
            else
            {
                // TODO YAMl check
            }
        }
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

    /// <summary>
    /// All routes to verify.
    /// </summary>
    /// <returns>OpenAPI endpoints.</returns>
    protected abstract IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints();

    /// <summary>
    /// Validates that the provided string is valid JSON and a valid OpenAPI document.
    /// </summary>
    /// <param name="openApiJson">The OpenAPI JSON string to validate.</param>
    private static void ValidateJsonOpenApiDocument(string openApiJson)
    {
        // First, validate it's valid JSON
        JsonDocument jsonDoc;
        try
        {
            jsonDoc = JsonDocument.Parse(openApiJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON: {ex.Message}", ex);
        }

        // Validate basic OpenAPI structure
        using (jsonDoc)
        {
            var root = jsonDoc.RootElement;

            // Check for required OpenAPI fields
            Assert.True(root.TryGetProperty("openapi", out var versionElement), "OpenAPI document must have 'openapi' version field");

            var version = versionElement.GetString();
            Assert.NotNull(version);
            Assert.True(root.TryGetProperty("info", out _), "OpenAPI document must have 'info' section");
            Assert.True(root.TryGetProperty("paths", out _), "OpenAPI document must have 'paths' section");
        }
    }
}
