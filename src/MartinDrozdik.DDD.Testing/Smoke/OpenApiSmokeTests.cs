using System.Text.Json;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Base class for smoke tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
public abstract partial class OpenApiSmokeTests<TProgram> : IDisposable
    where TProgram : class
{
    private readonly TestedApp<TProgram> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSmokeTests{TProgram}"/> class.
    /// </summary>
    /// <param name="builder">App factory under test.</param>
    protected OpenApiSmokeTests(TestedAppBuilder<TProgram> builder)
    {
        _factory = builder.Build();
    }

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
            var response = await client.GetAsync(openApi.Url, TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            if (openApi.Type == OpenApiType.Json)
            {
                ValidateJsonOpenApiDocument(content);
            }
            else
            {
                ValidateYamlOpenApiDocument(content);
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

    /// <summary>
    /// Validates that the provided string is valid YAML and a valid OpenAPI document.
    /// </summary>
    /// <param name="openApiYaml">The OpenAPI YAML string to validate.</param>
    private static void ValidateYamlOpenApiDocument(string openApiYaml)
    {
        // First, validate it's valid YAML
        var yamlStream = new YamlStream();
        try
        {
            using var reader = new StringReader(openApiYaml);
            yamlStream.Load(reader);
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"Invalid YAML: {ex.Message}", ex);
        }

        // Validate the document is non-empty and has a root mapping node
        Assert.True(yamlStream.Documents.Count > 0, "YAML document must not be empty");

        var root = yamlStream.Documents[0].RootNode;
        Assert.True(root is YamlMappingNode, "OpenAPI YAML root must be a mapping node");

        var rootMapping = (YamlMappingNode)root;

        // Helper to find a key in the root mapping (case-sensitive, per OpenAPI spec)
        bool HasKey(string key) => rootMapping.Children.Keys.OfType<YamlScalarNode>().Any(k => k.Value == key);

        // Check for required OpenAPI fields
        Assert.True(HasKey("openapi"), "OpenAPI document must have 'openapi' version field");
        Assert.True(HasKey("info"), "OpenAPI document must have 'info' section");
        Assert.True(HasKey("paths"), "OpenAPI document must have 'paths' section");

        // Validate the openapi version value is a non-empty string
        var versionNode = rootMapping.Children
            .FirstOrDefault(kvp => kvp.Key is YamlScalarNode { Value: "openapi" })
            .Value as YamlScalarNode;

        Assert.NotNull(versionNode);
        Assert.False(string.IsNullOrWhiteSpace(versionNode.Value), "'openapi' version field must not be empty");
    }
}
