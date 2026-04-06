using System.Net;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Represents a test case for an HTTP endpoint, including the HTTP method and the target URL.
/// </summary>
/// <param name="Method">The HTTP method to use when testing the endpoint.</param>
/// <param name="Url">The URL of the endpoint to be tested. You can use <see cref="UriBuilder"/> or <see cref="Integrations.UrlBuilder"/>.</param>
public record EndpointTest(HttpMethod Method, string Url)
{
    /// <summary>
    /// Gets acceptable status codes for this endpoint test.
    /// Besides 2xx status codes, can we consider other status codes as acceptable?
    /// Smoke tests are not meant to be strict, so we can allow some flexibility here.
    /// </summary>
    /// <example>
    /// If an endpoint is protected by authentication, we might expect 401 or 403 status codes, and that would be perfectly fine for a smoke test.
    /// </example>
    public IEnumerable<HttpStatusCode> AcceptableCodes { get; init; } = [];

    /// <summary>
    /// Gets contents to send with the request.
    /// Only applicable for methods that support a body (e.g., POST, PUT, PATCH).
    /// For GET and DELETE requests, this property will be ignored.
    /// </summary>
    public HttpContent? Content { get; init; }

    /// <summary>
    /// Gets an optional assertion action to perform on the <see cref="HttpResponseMessage"/> returned by the endpoint.
    /// </summary>
    public Action<HttpResponseMessage> Assert { get; init; } = _ => { };

    /// <summary>
    /// Adds additional acceptable status codes to this endpoint test.
    /// </summary>
    /// <param name="codes">Additional acceptable codes.</param>
    /// <returns>New instance of <see cref="EndpointTest"/>.</returns>
    public EndpointTest WithAcceptableCodes(params IEnumerable<HttpStatusCode> codes)
    {
        return new EndpointTest(Method, Url)
        {
            AcceptableCodes = AcceptableCodes.Concat(codes),
        };
    }

    /// <inheritdoc />
    public override string ToString() => $"{Method} {Url}";
}
