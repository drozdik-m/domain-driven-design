using MartinDrozdik.DDD.Blobs;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// How the file store keeps one <see cref="BlobContainer"/>.
/// </summary>
public sealed class FileBlobContainerOptions
{
    /// <summary>
    /// Gets or sets the folder the content of the container is kept in.
    /// </summary>
    /// <remarks>
    /// Exactly where the files land - <c>{Path}/{blobId}</c>, nothing added.
    /// No other container may use the same folder, or one inside it.
    /// Cached on first write.
    /// </remarks>
    public string Path { get; set; } = string.Empty;
}
