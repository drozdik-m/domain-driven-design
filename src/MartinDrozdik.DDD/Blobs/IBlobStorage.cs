using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Results;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// Stores and retrieves files together with their catalogue rows.
/// </summary>
/// <remarks>
/// <para>
/// This is the service applications primarily use.
/// Uses<see cref="IBlobStore"/> to "move bytes" and the <see cref="Blob"/> table to track them.
/// </para>
/// </remarks>
public interface IBlobStorage
{
    /// <summary>
    /// Writes the content of a blob and tracks its catalogue row.
    /// Does maximum effort to store the content atomically with the row and prevent lingering content when the row is never saved (via outbox).
    /// </summary>
    /// <remarks>
    /// The row is only tracked - the caller's save commits it.
    /// The content, however, is written straight away and removed through the outbox after a lease period unless that save happens within the grace period.
    /// </remarks>
    /// <param name="request">What to store.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The catalogued blob, or an <see cref="Error"/> when the content was refused.</returns>
    /// <exception cref="Exceptions.BlobUploadReclaimedException">The upload took longer than the grace period and its content is being removed.</exception>
    Task<IResult<Blob, Error>> AddOnSaveAsync(BlobUploadRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the catalogue row of a blob.
    /// </summary>
    /// <param name="id">The identity to look up.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The blob, or an <see cref="Error"/> when nothing is catalogued under the identity.</returns>
    Task<IResult<Blob, Error>> GetAsync(BlobId id, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the content of a blob, with its catalogue row attached.
    /// </summary>
    /// <remarks>
    /// A row whose content is missing from the store fails with <see cref="BlobErrorCodes.BlobContentMissing"/>.
    /// </remarks>
    /// <param name="id">The identity to read.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The content and its row, or an <see cref="Error"/>.</returns>
    Task<IResult<BlobContent, Error>> OpenReadAsync(BlobId id, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a blob and enqueues the deletion of its content, both inside the current transaction.
    /// </summary>
    /// <remarks>
    /// Nothing happens until the caller saves.
    /// If the transaction rolls back, the row and the content remain.
    /// If it commits, the row is gone and the content is scheduled for removal through the outbox.
    /// <para>
    /// An aggregate with several files calls this once per blob it holds - one message each.
    /// </para>
    /// </remarks>
    /// <param name="blob">The blob to remove.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>A task that completes once the removal is tracked.</returns>
    /// <example>
    /// <code>
    /// context.Invoices.Remove(invoice);
    /// await blobStorage.DeleteOnSaveAsync(scan, cancellationToken);
    /// await context.SaveChangesAsync(cancellationToken); // both, or neither
    /// </code>
    /// </example>
    Task DeleteOnSaveAsync(Blob blob, CancellationToken cancellationToken);
}
