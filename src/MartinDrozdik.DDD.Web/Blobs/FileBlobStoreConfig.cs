using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Gives every container its folder in the file store.
/// </summary>
/// <param name="services">Services to register the options to.</param>
public sealed class FileBlobStoreConfig(IServiceCollection services)
{
    private readonly HashSet<BlobContainer> _containers = [];

    /// <summary>
    /// Keeps the content of a container in a folder.
    /// </summary>
    /// <remarks>
    /// The folder is exactly where the files land - <c>{path}/{blobId}</c>, nothing added. Every container has a
    /// folder of its own: two containers sharing one, or one nested inside another, fail at startup. Several
    /// instances share each folder, over a network share or a shared volume, never a folder each.
    /// <para>
    /// Changing the folder of a container does not move what is already stored - that is an operator's job.
    /// </para>
    /// </remarks>
    /// <param name="container">The container, registered with <see cref="BlobsConfig.WithContainer"/>.</param>
    /// <param name="path">The folder its content is kept in. Created on first write if it is not there.</param>
    /// <returns>This for chaining.</returns>
    /// <exception cref="BlobException">The container is already configured.</exception>
    public FileBlobStoreConfig WithContainer(BlobContainer container, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return WithContainer(container, options => options.Path = path);
    }

    /// <summary>
    /// Configures how the file store keeps a container.
    /// </summary>
    /// <param name="container">The container, registered with <see cref="BlobsConfig.WithContainer"/>.</param>
    /// <param name="configure">Action to configure the container.</param>
    /// <returns>This for chaining.</returns>
    /// <exception cref="BlobException">The container is already configured.</exception>
    public FileBlobStoreConfig WithContainer(BlobContainer container, Action<FileBlobContainerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(configure);

        if (!_containers.Add(container))
        {
            throw new BlobException($"Blob container '{container}' is configured in the file store twice. Each container is kept in exactly one place.");
        }

        var containerOptions = new FileBlobContainerOptions();
        configure(containerOptions);
        services.Configure<FileBlobOptions>(options => options.Containers.Add(container, containerOptions));

        return this;
    }
}
