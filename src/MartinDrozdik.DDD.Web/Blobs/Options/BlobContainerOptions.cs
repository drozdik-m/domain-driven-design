using MartinDrozdik.DDD.Blobs;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Behaviour of one <see cref="BlobContainer"/>.
/// </summary>
/// <remarks>
/// Containers are separate by design.
/// </remarks>
public sealed class BlobContainerOptions
{
    /// <summary>
    /// Gets or sets the largest content that may be stored, in bytes, or null for no limit.
    /// </summary>
    /// <remarks>
    /// Enforced as the content streams past.
    /// It is not a substitute for the request body limit of the web server, which stops the bytes even earlier.
    /// </remarks>
    public long? MaxSize { get; set; }

    /// <summary>
    /// Gets or sets the media types that may be stored, or null to accept any.
    /// </summary>
    /// <remarks>
    /// Compared against the type the caller supplied.
    /// The library never inspects content to guess what it is.
    /// </remarks>
    public IReadOnlySet<string>? AllowedContentTypes { get; set; }

    /// <summary>
    /// Gets or sets the file extensions that may be stored, lowercase and without the leading dot, or null to accept any.
    /// </summary>
    /// <remarks>
    /// Once a list is set, a file that arrived without an extension is refused too.
    /// </remarks>
    public IReadOnlySet<string>? AllowedExtensions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content is hashed while it is written.
    /// </summary>
    /// <remarks>
    /// On by default. The hash costs a pass that is already being made.
    /// </remarks>
    public bool ComputeChecksum { get; set; } = true;

    /// <summary>
    /// Gets or sets how fast an upload has to commit its row before its content is removed as an orphan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keep it comfortably longer than the slowest transaction that could be holding a blob.
    /// </para>
    /// <para>
    /// This is what protects an upload whose transaction has not committed yet.
    /// The file is written before the row is, so every upload schedules the removal of its content through the outbox first in case the transaction fails.
    /// Saving the transaction removes the scheduled removal.
    /// </para>
    /// </remarks>
    public TimeSpan OrphanGracePeriod { get; set; } = TimeSpan.FromHours(24);
}
