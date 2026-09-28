using MartinDrozdik.DDD.Blobs.Checksums;

namespace MartinDrozdik.DDD.Blobs.Models;

/// <summary>
/// Everything <see cref="Blob.Create"/> needs to catalogue a blob whose content has already been written.
/// </summary>
public sealed record CreateBlobParams
{
    /// <summary>
    /// Gets the address the content was written to.
    /// </summary>
    public required BlobKey Key { get; init; }

    /// <summary>
    /// Gets the name the file arrived with.
    /// </summary>
    /// <remarks>
    /// Must not be empty. Anything else is accepted and shortened to <see cref="Blob.OriginalFileNameMaxLength"/>.
    /// </remarks>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Gets the media type the content was declared as.
    /// </summary>
    public required MediaType ContentType { get; init; }

    /// <summary>
    /// Gets the number of bytes written.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the UTC time the blob was catalogued.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the hash of what was written, or null when hashing was turned off.
    /// </summary>
    public required BlobChecksum? Checksum { get; init; }

    /// <summary>
    /// Gets the tags to store alongside the blob.
    /// </summary>
    public required BlobMetadata Metadata { get; init; }

    /// <summary>
    /// Gets who stored the blob, or null.
    /// </summary>
    /// <remarks>
    /// At most <see cref="Blob.CreatedByMaxLength"/> characters.
    /// </remarks>
    public required string? CreatedBy { get; init; }

    /// <summary>
    /// Gets the UTC time after which the blob may be swept, or null to keep it indefinitely.
    /// </summary>
    public required DateTime? ExpiresAtUtc { get; init; }
}
