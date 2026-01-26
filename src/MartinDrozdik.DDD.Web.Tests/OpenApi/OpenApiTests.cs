using System.Text.Json;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.OpenApi;

public class OpenApiTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task App_returns_hello_world()
    {
        // Arrange
        var factory = new TestAppFactory(testOutputHelper);
        var client = factory.CreateClient();

        // Act
        var result = await client.GetAsync("/openapi/doc.json");

        // Assert
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();
        ValidateOpenApiDocument(content);
    }

    /// <summary>
    /// Validates that the provided string is valid JSON and a valid OpenAPI document.
    /// </summary>
    /// <param name="openApiJson">The OpenAPI JSON string to validate.</param>
    private static void ValidateOpenApiDocument(string openApiJson)
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
            Assert.True(version.StartsWith("3."), $"Expected OpenAPI version 3.x, got: {version}");

            Assert.True(root.TryGetProperty("info", out _), "OpenAPI document must have 'info' section");

            Assert.True(root.TryGetProperty("paths", out _), "OpenAPI document must have 'paths' section");
        }
    }
}
