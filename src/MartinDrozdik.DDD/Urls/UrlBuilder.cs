using System.Text;

namespace MartinDrozdik.DDD.Urls;

/// <summary>
/// Builder for constructing URLs with support for path segments, query parameters, fragments, and parameter replacement.
/// The build is idempotent and independent.
/// </summary>
public class UrlBuilder
{
    private bool _relative = false;
    private readonly List<string> _segments = [];
    private readonly List<QueryParameter> _queryParameters = [];
    private readonly List<ValueParameter> _parameters = [];
    private string? _fragment;
    private string? _domain;
    private int? _port;
    private string? _scheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlBuilder"/> class.
    /// </summary>
    /// <param name="segments">The initial path segments. Same operation as <see cref="WithPath(IEnumerable{string})"/>.</param>
    public UrlBuilder(params IEnumerable<string> segments)
    {
        WithPath(segments);
    }

    /// <summary>
    /// Adds more path segments to the resulting URL.
    /// </summary>
    /// <example>
    /// If the current URL is "/api/users" and you call <see cref="WithPath(IEnumerable{string})"/> with segments ["{id}", "details"], the resulting URL will be "/api/users/{id}/details".
    /// </example>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="segments">Segments to add.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithPath(IEnumerable<string> segments)
    {
        _segments.AddRange(segments);
        return this;
    }

    /// <summary>
    /// Adds a query parameter to the resulting URL.
    /// </summary>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="name">Key of the query parameter.</param>
    /// <param name="value">Value of the query parameter.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithQueryParameter(string name, string value)
    {
        _queryParameters.Add(new QueryParameter(name, value));
        return this;
    }

    /// <summary>
    /// Replaces parameters in format {parameterName} in the URL with the provided value.
    /// </summary>
    /// <param name="key">The "parameterName" inside {parameterName}.</param>
    /// <param name="value">The value to replace {parameterName} with.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithParameter(string key, string value)
    {
        _parameters.Add(new ValueParameter(key, value));
        return this;
    }

    /// <summary>
    /// Adds a fragment to the resulting URL.
    /// The fragment is the part of the URL that comes after the '#' character.
    /// </summary>
    /// <example>
    /// For example, in the URL "http://example.com/page#section1", the fragment is "section1".
    /// </example>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="fragment">The fragment value.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithFragment(string fragment)
    {
        _fragment = fragment;
        return this;
    }

    /// <summary>
    /// Sets the domain and optionally the port of the resulting URL.
    /// </summary>
    /// <remarks>Supports bracketed {parameters}.</remarks>
    /// <param name="domain">The domain.</param>
    /// <param name="port">The port.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithDomain(string domain, int? port)
    {
        _domain = domain;
        _port = port;
        return this;
    }

    /// <summary>
    /// Sets the scheme of the resulting URL (e.g., "http", "https").
    /// </summary>
    /// <param name="scheme">The scheme.</param>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder WithScheme(string scheme)
    {
        _scheme = scheme;
        return this;
    }

    /// <summary>
    /// Makes the resulting URL relative, omitting the leading '/' before the path.
    /// </summary>
    /// <remarks>
    /// Cannot be combined with a domain, as a domain always implies an absolute URL.
    /// </remarks>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder AsRelative()
    {
        _relative = true;
        return this;
    }

    /// <summary>
    /// Makes the resulting URL absolute, ensuring the leading '/' before the path.
    /// </summary>
    /// <returns>This <see cref="UrlBuilder"/> for chaining.</returns>
    public UrlBuilder AsAbsolute()
    {
        _relative = false;
        return this;
    }

    /// <summary>
    /// Builds the final URL string based on the provided data.
    /// Build is idempotent and independent.
    /// </summary>
    /// <returns>Final URL as string.</returns>
    public string Build()
    {
        if (_scheme is not null && _domain is null)
        {
            throw new InvalidOperationException("A scheme cannot be set without a domain.");
        }

        if (_relative && _domain is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a domain.");
        }

        if (_relative && _port is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a port.");
        }

        if (_relative && _scheme is not null)
        {
            throw new InvalidOperationException("A relative URL cannot have a scheme.");
        }

        var result = new StringBuilder();

        // Scheme + domain + port
        if (_domain is not null)
        {
            if (_scheme is not null)
            {
                result.Append(_scheme);
                result.Append("://");
            }

            result.Append(ApplyParameters(_domain));

            if (_port is not null)
            {
                result.Append(':');
                result.Append(_port);
            }
        }

        // Path segments
        if (_segments.Count > 0)
        {
            if (!_relative)
            {
                result.Append('/');
            }

            result.AppendJoin('/', ApplyParametersTexts(_segments).Select(Uri.EscapeDataString));
        }

        // Query string
        if (_queryParameters.Count > 0)
        {
            var queryPairs = _queryParameters.Select(q => $"{Uri.EscapeDataString(ApplyParameters(q.Name))}={Uri.EscapeDataString(ApplyParameters(q.Value))}");
            result.Append('?');
            result.AppendJoin('&', queryPairs);
        }

        // Fragment
        if (_fragment is not null)
        {
            result.Append('#');
            result.Append(Uri.EscapeDataString(ApplyParameters(_fragment)));
        }

        return result.ToString();

        string ApplyParameters(string text)
        {
            var result = text;
            foreach (var parameter in _parameters)
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
