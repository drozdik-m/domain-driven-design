using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// The containers registered with blob storage, captured at startup.
/// </summary>
/// <remarks>
/// The registry is what makes that checkable - by blob storage before every upload, by the store at startup, and by smoke tests.
/// </remarks>
public sealed class BlobContainerRegistry
{
    private readonly List<BlobContainer> _containers = [];

    /// <summary>
    /// Gets every registered container, in registration order.
    /// </summary>
    public IReadOnlyCollection<BlobContainer> Containers => _containers;

    /// <summary>
    /// Tells if a container is registered.
    /// </summary>
    /// <param name="container">The container to look for.</param>
    /// <returns>True when the container is registered, else false.</returns>
    public bool Contains(BlobContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return _containers.Contains(container);
    }

    /// <summary>
    /// Records a container.
    /// </summary>
    /// <param name="container">The container to record.</param>
    /// <exception cref="BlobException">The container is already registered.</exception>
    internal void Add(BlobContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (Contains(container))
        {
            throw new BlobException($"Blob container '{container}' is registered twice. Each container is configured in exactly one place.");
        }

        _containers.Add(container);
    }
}
