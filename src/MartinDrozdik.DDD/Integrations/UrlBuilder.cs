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
    /// Deconstructs existing URL for futher modifications.
    /// Supports both absolute and relative URLs.
    /// </summary>
    /// <remarks>
    /// Completely ignores {parameters}.
    /// The new builder will not contain any {parameters} from the original URL, even if they were present in the path, query or fragment.
    /// </remarks>
    /// <param name="url">The url string to parse.</param>
    /// <returns>New <see cref="UrlBuilder"/> with information from the original <paramref name="url"/>.</returns>
    public static UrlBuilder FromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        // Absolute URL
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            var segments = absoluteUri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString);

            var queryParameters = ParseQuery(absoluteUri.Query);

            return new UrlBuilder(segments)
            {
                Scheme = absoluteUri.Scheme,
                Domain = absoluteUri.Host,
                Port = absoluteUri.IsDefaultPort
                    ? null
                    : absoluteUri.Port,
                Fragment = string.IsNullOrEmpty(absoluteUri.Fragment)
                    ? null
                    : Uri.UnescapeDataString(absoluteUri.Fragment.TrimStart('#')),
                QueryParameters = [.. queryParameters],
                Relative = false,
            };
        }

        // Possibly malformed absolute URL
        if (url.Contains("://") && !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid absolute URL format.", nameof(url));
        }

        // Other cases (relative or absolute starting with '/')
        if (Uri.TryCreate(url, UriKind.Relative, out var relativeUri))
        {
            var isRelative = !url.StartsWith('/');
            var raw = relativeUri.OriginalString;

            // Fragment
            string? fragment = null;
            var hashIndex = raw.IndexOf('#');
            if (hashIndex >= 0)
            {
                fragment = Uri.UnescapeDataString(raw[(hashIndex + 1)..]);
                raw = raw[..hashIndex];
            }

            // Query
            var queryPart = string.Empty;
            var queryIndex = raw.IndexOf('?');
            if (queryIndex >= 0)
            {
                queryPart = raw[(queryIndex + 1)..];
                raw = raw[..queryIndex];
            }

            var segments = raw
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString);

            var queryParameters = ParseQuery(queryPart);

            return new UrlBuilder(segments)
            {
                Fragment = fragment,
                QueryParameters = [.. queryParameters],
                Relative = isRelative,
            };
        }

        throw new ArgumentException("Invalid URL format.", nameof(url));
    }

    /// <summary>
    /// Parses an URL query section.
    /// </summary>
    /// <param name="query">The query to parse. May start with "?" and contain queries joined via "&amp;".</param>
    /// <returns>List of parsed <see cref="QueryParameter"/>.</returns>
    private static IEnumerable<QueryParameter> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            yield break;
        }

        var trimmed = query.TrimStart('?');

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');

            if (idx >= 0)
            {
                var name = Uri.UnescapeDataString(pair[..idx]);
                var value = Uri.UnescapeDataString(pair[(idx + 1)..]);
                yield return new QueryParameter(name, value);
            }
            else
            {
                // key without value (?flag)
                var name = Uri.UnescapeDataString(pair);
                yield return new QueryParameter(name, string.Empty);
            }
        }
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
    public UrlBuilder WithPath(params IEnumerable<string> segments) =>
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

        if (QueryParameters.Any(e => string.IsNullOrEmpty(e.Name)))
        {
            var emptyKeys = QueryParameters.Where(e => string.IsNullOrEmpty(e.Name)).Select(e => e.Value);
            throw new InvalidOperationException($"Empty keys for values {string.Join(", ", emptyKeys)} are not allowed.");
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
        else if (!Relative && Domain is null)
        {
            result.Append('/');
        }

        // Query string
        if (QueryParameters.Count > 0)
        {
            var queryPairs = QueryParameters.Select(q => $"{Uri.EscapeDataString(ApplyParameters(q.Name))}={Uri.EscapeDataString(ApplyParameters(q.Value))}");
            result.Append('?');
            result.AppendJoin('&', queryPairs);
        }

        // Fragment
        if (Fragment is not null && !string.IsNullOrEmpty(Fragment))
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
