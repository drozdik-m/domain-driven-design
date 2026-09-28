using MartinDrozdik.DDD.Blobs.Models;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// What to store when adding a blob.
/// </summary>
public sealed record BlobUploadRequest
{
    /// <summary>
    /// Gets the container to store the blob in.
    /// </summary>
    public required BlobContainer Container { get; init; }

    /// <summary>
    /// Gets the content to store.
    /// </summary>
    /// <remarks>
    /// Read forwards exactly once. A request body is fine; it is never rewound or asked for its length.
    /// </remarks>
    public required Stream Content { get; init; }

    /// <summary>
    /// Gets the name the file arrived with.
    /// </summary>
    /// <remarks>
    /// Recorded as it is, with a sanitized copy in <see cref="Blob.Name"/>. Anything at all is accepted here.
    /// </remarks>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Gets the media type of the content, as declared by the application.
    /// </summary>
    /// <remarks>
    /// Never guessed from the content.
    /// </remarks>
    public required MediaType ContentType { get; init; }

    /// <summary>
    /// Gets the tags to store alongside the blob.
    /// </summary>
    public BlobMetadata Metadata { get; init; } = BlobMetadata.Empty;

    /// <summary>
    /// Gets who is storing the blob, or null to record nobody.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Gets the instant after which the sweep may remove the blob, or null to keep it indefinitely.
    /// </summary>
    /// <remarks>
    /// For content that is temporary by design.
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; init; }
}
