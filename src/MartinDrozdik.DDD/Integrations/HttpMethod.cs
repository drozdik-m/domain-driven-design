using MartinDrozdik.DDD.Enumerations;

namespace MartinDrozdik.DDD.Integrations;

/// <summary>
/// Enumeration of HTTP methods.
/// </summary>
/// <param name="name">Uppercase identifier of the method.</param>
public class HttpMethod(EnumerationName name) : StaticEnumeration<HttpMethod>(name)
{
    /// <summary>
    /// Gets the GET HTTP method.
    /// </summary>
    public static HttpMethod Get { get; } = new HttpMethod("GET");

    /// <summary>
    /// Gets the HEAD HTTP method.
    /// </summary>
    public static HttpMethod Head { get; } = new HttpMethod("HEAD");

    /// <summary>
    /// Gets the OPTIONS HTTP method.
    /// </summary>
    public static HttpMethod Options { get; } = new HttpMethod("OPTIONS");

    /// <summary>
    /// Gets the TRACE HTTP method.
    /// </summary>
    public static HttpMethod Trace { get; } = new HttpMethod("TRACE");

    /// <summary>
    /// Gets the PUT HTTP method.
    /// </summary>
    public static HttpMethod Put { get; } = new HttpMethod("PUT");

    /// <summary>
    /// Gets the DELETE HTTP method.
    /// </summary>
    public static HttpMethod Delete { get; } = new HttpMethod("DELETE");

    /// <summary>
    /// Gets the POST HTTP method.
    /// </summary>
    public static HttpMethod Post { get; } = new HttpMethod("POST");

    /// <summary>
    /// Gets the PATCH HTTP method.
    /// </summary>
    public static HttpMethod Patch { get; } = new HttpMethod("PATCH");

    /// <summary>
    /// Gets the CONNECT HTTP method.
    /// </summary>
    public static HttpMethod Connect { get; } = new HttpMethod("CONNECT");
}
