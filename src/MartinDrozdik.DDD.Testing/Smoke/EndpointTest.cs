using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using Xunit.Sdk;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Represents a test case for an HTTP endpoint, including the HTTP method and the target URL.
/// </summary>
/// <remarks>
/// Must be serializable to allow better test exploration and reporting.
/// </remarks>
/// <param name="method">The HTTP method to use when testing the endpoint.</param>
/// <param name="url">The URL of the endpoint to be tested. You can use <see cref="UriBuilder"/> or <see cref="Integrations.UrlBuilder"/>.</param>
[DebuggerDisplay("{Method} {Url}")]
public class EndpointTest(HttpMethod method, string url) : IXunitSerializable
{
    /// <summary>
    /// Gets the HTTP method to use when testing the endpoint.
    /// </summary>
    public HttpMethod Method { get; private set; } = method;

    /// <summary>
    /// Gets the URL of the endpoint to be tested.
    /// </summary>
    public string Url { get; private set; } = url;

    /// <summary>
    /// Gets or sets acceptable status codes for this endpoint test.
    /// Besides 2xx status codes, can we consider other status codes as acceptable?
    /// Smoke tests are not meant to be strict, so we can allow some flexibility here.
    /// </summary>
    /// <example>
    /// If an endpoint is protected by authentication, we might expect 401 or 403 status codes, and that would be perfectly fine for a smoke test.
    /// </example>
    public IEnumerable<HttpStatusCode> AcceptableCodes { get; set; } = [];

    /// <summary>
    /// Gets or sets contents to send with the request.
    /// Only applicable for methods that support a body (e.g., POST, PUT, PATCH).
    /// For GET and DELETE requests, this property will be ignored.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets Mime type for the <see cref="Content"/>.
    /// </summary>
    public string ContentType { get; set; } = MediaTypeNames.Application.Json;

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
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Method), Method.Method);
        info.AddValue(nameof(Url), Url);
        info.AddValue(nameof(Content), Content);
        info.AddValue(nameof(ContentType), ContentType);
        info.AddValue(nameof(AcceptableCodes), AcceptableCodes.Select(e => (int)e).ToArray());
    }

    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        var methodString = info.GetValue<string>(nameof(Method));
        ArgumentNullException.ThrowIfNull(methodString);

        var urlString = info.GetValue<string>(nameof(Url));
        ArgumentNullException.ThrowIfNull(urlString);

        var acceptableCodesInts = info.GetValue<int[]>(nameof(AcceptableCodes));
        ArgumentNullException.ThrowIfNull(acceptableCodesInts);

        var contentTypeString = info.GetValue<string>(nameof(ContentType));
        ArgumentNullException.ThrowIfNull(contentTypeString);

        Method = new HttpMethod(methodString);
        Url = urlString;
        AcceptableCodes = acceptableCodesInts.Select(c => (HttpStatusCode)c);
        Content = info.GetValue<string?>(nameof(Content));
        ContentType = contentTypeString;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Method} {Url}";
}
