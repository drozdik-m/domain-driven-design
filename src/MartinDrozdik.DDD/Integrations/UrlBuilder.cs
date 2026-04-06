using System.Collections.Immutable;
using System.Text;

namespace MartinDrozdik.DDD.Integrations;

/// <summary>
/// Builder for constructing URLs with support for path segments, query parameters, fragments, and parameter replacement.
/// The build is immutable, idempotent and independent.
/// </summary>
/// <param name="initialSegments">The initial path segments. Same operation as <see cref="WithPath(IEnumerable{string})"/>.</param>
public record UrlBuilder(params IEnumerable<string> initialSegments)
{
    private ImmutableList<string> Segments { get; init; } = [.. initialSegments];

    private ImmutableList<QueryParameter> QueryParameters { get; init; } = [];

    private ImmutableList<ValueParameter> Parameters { get; init; } = [];

    private bool Relative { get; init; }

    private string? Fragment { get; init; }

    private string? Domain { get; init; }

    private int? Port { get; init; }

    private string? Scheme { get; init; }

    /// <summary>
    /// Implicit conversion to string via <see cref="Build"/>.
    /// </summary>
    /// <param name="builder">The builder to use.</param>
    public static implicit operator string(UrlBuilder builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Adds more path segments to the resulting URL.
    /// </summary>
    /// <example>
    /// If the current URL is "/api/users" and you call <see cref="WithPath(IEnumerable{string})"/> with segments ["{id}", "details"], the resulting URL will be "/api/users/{id}/details".
    /// </example>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="segments">Segments to add.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the segments appended.</returns>
    public UrlBuilder WithPath(IEnumerable<string> segments) =>
        this with { Segments = Segments.AddRange(segments) };

    /// <summary>
    /// Adds a query parameter to the resulting URL.
    /// </summary>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="name">Key of the query parameter.</param>
    /// <param name="value">Value of the query parameter.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the query parameter added.</returns>
    public UrlBuilder WithQueryParameter(string name, string value) =>
        this with { QueryParameters = QueryParameters.Add(new QueryParameter(name, value)) };

    /// <summary>
    /// Replaces parameters in format {parameterName} in the URL with the provided value.
    /// </summary>
    /// <param name="key">The "parameterName" inside {parameterName}.</param>
    /// <param name="value">The value to replace {parameterName} with.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the parameter registered.</returns>
    public UrlBuilder WithParameter(string key, string value) =>
        this with { Parameters = Parameters.Add(new ValueParameter(key, value)) };

    /// <summary>
    /// Adds a fragment to the resulting URL.
    /// The fragment is the part of the URL that comes after the '#' character.
    /// </summary>
    /// <example>
    /// For example, in the URL "http://example.com/page#section1", the fragment is "section1".
    /// </example>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="fragment">The fragment value.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the fragment set.</returns>
    public UrlBuilder WithFragment(string fragment) =>
        this with { Fragment = fragment };

    /// <summary>
    /// Sets the domain and optionally the port of the resulting URL.
    /// </summary>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="domain">The domain.</param>
    /// <param name="port">The port.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the domain and port set.</returns>
    public UrlBuilder WithDomain(string domain, int? port = null) =>
        this with { Domain = domain, Port = port };

    /// <summary>
    /// Sets the scheme of the resulting URL (e.g., "http", "https").
    /// </summary>
    /// <param name="scheme">The scheme.</param>
    /// <returns>A new <see cref="UrlBuilder"/> with the scheme set.</returns>
    public UrlBuilder WithScheme(string scheme) =>
        this with { Scheme = scheme };

    /// <summary>
    /// Makes the resulting URL relative, omitting the leading '/' before the path.
    /// </summary>
    /// <remarks>
    /// Cannot be combined with a domain, as a domain always implies an absolute URL.
    /// </remarks>
    /// <returns>A new <see cref="UrlBuilder"/> marked as relative.</returns>
    public UrlBuilder AsRelative() =>
        this with { Relative = true };

    /// <summary>
    /// Makes the resulting URL absolute, ensuring the leading '/' before the path.
    /// </summary>
    /// <returns>A new <see cref="UrlBuilder"/> marked as absolute.</returns>
    public UrlBuilder AsAbsolute() =>
        this with { Relative = false };

    /// <summary>
    /// Builds the final URL string based on the provided data.
    /// Build is idempotent and independent.
    /// </summary>
    /// <returns>Final URL as string.</returns>
    public string Build()
    {
        if (Scheme is not null && Domain is null)
        {
            throw new InvalidOperationException("A scheme cannot be set without a domain.");
        }

        if (Relative && Domain is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a domain.");
        }

        if (Relative && Port is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a port.");
        }

        if (Relative && Scheme is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a scheme.");
        }

        var result = new StringBuilder();

        // Scheme + domain + port
        if (Domain is not null)
        {
            if (Scheme is not null)
            {
                result.Append(Scheme);
                result.Append("://");
            }

            result.Append(ApplyParameters(Domain));

            if (Port is not null)
            {
                result.Append(':');
                result.Append(Port);
            }
        }

        // Path segments
        if (Segments.Count > 0)
        {
            if (!Relative)
            {
                result.Append('/');
            }

            result.AppendJoin('/', ApplyParametersTexts(Segments).Select(Uri.EscapeDataString));
        }

        // Query string
        if (QueryParameters.Count > 0)
        {
            var queryPairs = QueryParameters.Select(q => $"{Uri.EscapeDataString(ApplyParameters(q.Name))}={Uri.EscapeDataString(ApplyParameters(q.Value))}");
            result.Append('?');
            result.AppendJoin('&', queryPairs);
        }

        // Fragment
        if (Fragment is not null)
        {
            result.Append('#');
            result.Append(Uri.EscapeDataString(ApplyParameters(Fragment)));
        }

        return result.ToString();

        string ApplyParameters(string text)
        {
            var result = text;
            foreach (var parameter in Parameters)
            {
                result = result.Replace($"{{{parameter.Key}}}", parameter.Value);
            }

            return result;
        }

        IEnumerable<string> ApplyParametersTexts(IEnumerable<string> texts)
        {
            return texts.Select(ApplyParameters);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Build();
    }

    private record struct QueryParameter(string Name, string Value);

    private record struct ValueParameter(string Key, string Value);
}
