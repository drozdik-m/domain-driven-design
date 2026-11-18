using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// An upload that is prepared but whose content is not written yet.
/// </summary>
/// <remarks>
/// Returned by <see cref="IBlobStorage{TContext}.PrepareUpload"/>.
/// Save your context, then hand it to <see cref="IBlobStorage{TContext}.AddOnSaveAsync"/>.
/// </remarks>
public sealed record BlobUpload
{
    /// <summary>
    /// Gets what to store.
    /// </summary>
    public required BlobUploadRequest Request { get; init; }

    /// <summary>
    /// Gets the address the content will be written to.
    /// </summary>
    public required BlobKey Key { get; init; }

    /// <summary>
    /// Gets the ticket of the outbox message that removes the content unless the upload is completed in time.
    /// </summary>
    public required OutboxTicket Cleanup { get; init; }
}
