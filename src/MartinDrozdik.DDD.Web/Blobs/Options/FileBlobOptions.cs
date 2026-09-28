using MartinDrozdik.DDD.Blobs;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// How the file store keeps content.
/// </summary>
internal sealed class FileBlobOptions
{
    /// <summary>
    /// Gets the options of every container, exactly as configured.
    /// </summary>
    public Dictionary<BlobContainer, FileBlobContainerOptions> Containers { get; } = [];
}
