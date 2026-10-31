using Microsoft.AspNetCore.Http;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// An old URL path that sends the client to a new location.
/// </summary>
public sealed record Redirect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Redirect"/> class.
    /// </summary>
    /// <param name="from">
    /// The old path, starting with <c>/</c>, without a query string or fragment, e.g. <c>/contact.html</c>.
    /// It is matched against the request path ignoring case, percent-encoded or not.
    /// </param>
    /// <param name="to">
    /// The new location: a path starting with <c>/</c> (it may carry a query string and fragment),
    /// resolved against the application's path base, or an absolute <c>http</c>/<c>https</c> URL.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="from"/> or <paramref name="to"/> is not a valid redirect path, or they point to the same path.</exception>
    public Redirect(string from, string to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

        if (!from.StartsWith('/') || from.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException($"The redirect source '{from}' must be a path that starts with a single '/'.", nameof(from));
        }

        if (from.Contains('?') || from.Contains('#'))
        {
            throw new ArgumentException($"The redirect source '{from}' must not contain a query string or a fragment, the path alone is matched.", nameof(from));
        }

        From = from;
        To = to;
        FromPath = PathString.FromUriComponent(from);

        if (to.StartsWith('/') && !to.StartsWith("//", StringComparison.Ordinal))
        {
            var fragmentStart = to.IndexOf('#', StringComparison.Ordinal);
            var beforeFragment = fragmentStart < 0 ? to : to[..fragmentStart];
            var queryStart = beforeFragment.IndexOf('?', StringComparison.Ordinal);

            ToPath = PathString.FromUriComponent(queryStart < 0 ? beforeFragment : beforeFragment[..queryStart]);
            ToQuery = queryStart < 0 ? QueryString.Empty : QueryString.FromUriComponent(beforeFragment[queryStart..]);
            ToFragment = fragmentStart < 0 ? FragmentString.Empty : FragmentString.FromUriComponent(to[fragmentStart..]);

            if (FromPath.Equals(ToPath))
            {
                throw new ArgumentException($"The redirect from '{from}' to '{to}' points to itself and would loop forever.", nameof(to));
            }
        }
        else if (Uri.TryCreate(to, UriKind.Absolute, out var absolute) && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            ToAbsolute = absolute;
        }
        else
        {
            throw new ArgumentException($"The redirect target '{to}' must be a path that starts with a single '/', or an absolute http or https URL.", nameof(to));
        }
    }

    /// <summary>
    /// Gets the old path as given.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets the new location as given.
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Gets <see cref="From"/> decoded, as it is compared with <see cref="HttpRequest.Path"/>.
    /// </summary>
    internal PathString FromPath { get; }

    /// <summary>
    /// Gets the path of a local <see cref="To"/>, or <see cref="PathString.Empty"/> when it is absolute.
    /// </summary>
    internal PathString ToPath { get; }

    /// <summary>
    /// Gets the query string of a local <see cref="To"/>.
    /// </summary>
    internal QueryString ToQuery { get; }

    /// <summary>
    /// Gets the fragment of a local <see cref="To"/>.
    /// </summary>
    internal FragmentString ToFragment { get; }

    /// <summary>
    /// Gets <see cref="To"/> when it is an absolute URL, else null.
    /// </summary>
    internal Uri? ToAbsolute { get; }

    /// <summary>
    /// Builds the <c>Location</c> header for a request that matched <see cref="From"/>.
    /// The request's query string is carried over when <see cref="To"/> has none of its own, so tracking parameters survive.
    /// </summary>
    /// <param name="request">The matched request.</param>
    /// <returns>The value of the <c>Location</c> header.</returns>
    internal string BuildLocationHeader(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ToAbsolute is not null)
        {
            if (!string.IsNullOrEmpty(ToAbsolute.Query) || !request.QueryString.HasValue)
            {
                return ToAbsolute.AbsoluteUri;
            }

            return new UriBuilder(ToAbsolute) { Query = request.QueryString.Value[1..] }.Uri.AbsoluteUri;
        }

        var query = ToQuery.HasValue ? ToQuery : request.QueryString;
        return request.PathBase.Add(ToPath).ToUriComponent() + query.ToUriComponent() + ToFragment.ToUriComponent();
    }
}
