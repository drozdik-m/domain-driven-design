using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Registers the containers blob storage knows, and configures the sweep.
/// </summary>
/// <param name="services">Services to register the options to.</param>
/// <param name="registry">The registry recording every registered container.</param>
public sealed class BlobsConfig(IServiceCollection services, BlobContainerRegistry registry)
{
    /// <summary>
    /// Registers a container with options of its own.
    /// </summary>
    /// <remarks>
    /// Every container the application stores into is registered here (or it later fails as unregisted).
    /// </remarks>
    /// <param name="container">The container to register.</param>
    /// <param name="configure">Action to configure the container, or null to keep the defaults.</param>
    /// <returns>This for chaining.</returns>
    /// <exception cref="BlobException">The container is already registered.</exception>
    public BlobsConfig WithContainer(BlobContainer container, Action<BlobContainerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(container);

        // Register the container
        registry.Add(container);

        // Register the options for the container
        var optionsBuilder = services.AddOptions<BlobContainerOptions>(container.Name);
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder.ValidateOnStart();

        return this;
    }

    /// <summary>
    /// Configures the sweep that removes expired blobs.
    /// </summary>
    /// <param name="configure">Action to configure the sweep.</param>
    /// <returns>This for chaining.</returns>
    public BlobsConfig WithSweep(Action<BlobSweepOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return this;
    }
}
