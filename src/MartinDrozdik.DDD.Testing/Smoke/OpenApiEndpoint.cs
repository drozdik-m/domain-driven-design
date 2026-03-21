namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Represents an OpenAPI endpoint to be tested.
/// </summary>
/// <param name="Url">Target URL.</param>
/// <param name="Type">Document type.</param>
public record OpenApiEndpoint(string Url, OpenApiType Type);
