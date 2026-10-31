using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Redirects;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <inheritdoc cref="AddPermanentRedirect{TBuilder}(TBuilder, Redirect)"/>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="from">The old path, see <see cref="Redirect(string, string)"/>.</param>
    /// <param name="to">The new location, see <see cref="Redirect(string, string)"/>.</param>
    public static TBuilder AddPermanentRedirect<TBuilder>(this TBuilder builder, string from, string to)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddPermanentRedirect(new Redirect(from, to));
    }

    /// <summary>
    /// Redirects an old path with <see cref="StatusCodes.Status301MovedPermanently"/>.
    /// Call <see cref="ApplicationBuilderExtensions.UseRedirects(IApplicationBuilder)"/> to put the redirects in the request pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">Type of <see cref="IHostApplicationBuilder"/> to configure.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="redirect">The redirect.</param>
    /// <returns><typeparamref name="TBuilder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">A redirect from the same path is already registered.</exception>
    public static TBuilder AddPermanentRedirect<TBuilder>(this TBuilder builder, Redirect redirect)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddRedirect(redirect, StatusCodes.Status301MovedPermanently);
    }

    /// <inheritdoc cref="AddTemporaryRedirect{TBuilder}(TBuilder, Redirect)"/>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="from">The old path, see <see cref="Redirect(string, string)"/>.</param>
    /// <param name="to">The new location, see <see cref="Redirect(string, string)"/>.</param>
    public static TBuilder AddTemporaryRedirect<TBuilder>(this TBuilder builder, string from, string to)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddTemporaryRedirect(new Redirect(from, to));
    }

    /// <summary>
    /// Redirects an old path with <see cref="StatusCodes.Status302Found"/>.
    /// Call <see cref="ApplicationBuilderExtensions.UseRedirects(IApplicationBuilder)"/> to put the redirects in the request pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">Type of <see cref="IHostApplicationBuilder"/> to configure.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="redirect">The redirect.</param>
    /// <returns><typeparamref name="TBuilder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">A redirect from the same path is already registered.</exception>
    public static TBuilder AddTemporaryRedirect<TBuilder>(this TBuilder builder, Redirect redirect)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddRedirect(redirect, StatusCodes.Status302Found);
    }

    private static TBuilder AddRedirect<TBuilder>(this TBuilder builder, Redirect redirect, int statusCode)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(redirect);

        if (!RedirectRegistry.TryFind(builder.Services, out var registry))
        {
            registry = new RedirectRegistry();
            builder.Services.AddSingleton(registry);
        }

        registry.Add(redirect, statusCode);
        return builder;
    }
}
