using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// The redirects of the application.
/// </summary>
internal sealed class RedirectRegistry
{
    // PathString compares ignoring case
    private readonly Dictionary<PathString, RedirectRegistration> _registrations = [];

    /// <summary>
    /// Finds the registry while the container is still being built.
    /// </summary>
    /// <param name="services">The services a redirect may have been added to.</param>
    /// <param name="registry">The registry, when a redirect has been added.</param>
    /// <returns>True when a redirect has been added, else false.</returns>
    internal static bool TryFind(IServiceCollection services, [NotNullWhen(true)] out RedirectRegistry? registry)
    {
        ArgumentNullException.ThrowIfNull(services);

        registry = services
            .Where(descriptor => descriptor.ServiceType == typeof(RedirectRegistry) && !descriptor.IsKeyedService)
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<RedirectRegistry>()
            .FirstOrDefault();

        return registry is not null;
    }

    /// <summary>
    /// Records a redirect.
    /// </summary>
    /// <param name="redirect">The redirect to record.</param>
    /// <param name="statusCode">The status code to answer with.</param>
    /// <exception cref="InvalidOperationException">A redirect from the same path is already registered.</exception>
    internal void Add(Redirect redirect, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(redirect);

        if (!_registrations.TryAdd(redirect.FromPath, new RedirectRegistration(redirect, statusCode)))
        {
            var existing = _registrations[redirect.FromPath].Redirect;
            throw new InvalidOperationException($"A redirect from '{redirect.From}' is already registered, to '{existing.To}'. Each old path may redirect to one location only.");
        }
    }

    /// <summary>
    /// Finds the redirect of a request path.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="registration">The redirect, when the path has one.</param>
    /// <returns>True when the path has a redirect, else false.</returns>
    internal bool TryGet(PathString path, [NotNullWhen(true)] out RedirectRegistration? registration)
    {
        return _registrations.TryGetValue(path, out registration);
    }
}
