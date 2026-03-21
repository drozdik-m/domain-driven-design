namespace MartinDrozdik.DDD.Testing.Smoke;

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
