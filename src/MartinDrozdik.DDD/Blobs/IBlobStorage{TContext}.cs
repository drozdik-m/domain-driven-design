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
/// Uses <see cref="IBlobStore"/> to "move bytes" and the <see cref="Blob"/> table to track them.
/// </para>
/// <para>
/// Every method takes the context that holds the catalogue. Pass the instance you save, e.g. one pointed at a tenant's database.
/// <typeparamref name="TContext"/> is the context the blob storage was added for, so the compiler refuses a context of another type.
/// </para>
/// </remarks>
/// <typeparam name="TContext">The context that holds the catalogue.</typeparam>
public interface IBlobStorage<TContext>
{
    /// <summary>
    /// Step 1 of an upload: checks the request and prepares the lease (removal of the content unless the upload is completed in time).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing is written yet.
    /// Save your context before <see cref="AddOnSaveAsync"/>.
    /// </para>
    /// </remarks>
    /// <param name="context">The context to track the removal in. Its save commits the removal.</param>
    /// <param name="request">What to store.</param>
    /// <returns>The prepared upload, or an <see cref="Error"/> when the content was refused.</returns>
    /// <exception cref="InvalidOperationException">The container of the request is not registered.</exception>
    /// <example>
    /// <code>
    /// var upload = blobStorage.PrepareUpload(context, request);
    /// await context.SaveChangesAsync(cancellationToken); // commits the lease (scheduled removal in case of failure)
    ///
    /// var blob = await blobStorage.AddOnSaveAsync(context, upload.Value, cancellationToken);
    /// invoice.AttachScan(blob.Value.Id);
    /// await context.SaveChangesAsync(cancellationToken); // the row and the aggregate, and cancels the lease (scheduled removal)
    /// </code>
    /// </example>
    IResult<BlobUpload, Error> PrepareUpload(TContext context, BlobUploadRequest request);

    /// <summary>
    /// Step 2 of an upload: writes the content and tracks its catalogue row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The row is only tracked and needs to be saved.
    /// The same save cancels the safety removal <see cref="PrepareUpload"/> scheduled.
    /// If it never happens, the removal deletes the content once the grace period ends.
    /// </para>
    /// </remarks>
    /// <param name="context">The context to track the row in. Its save commits the row.</param>
    /// <param name="upload">The upload <see cref="PrepareUpload"/> returned.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The catalogued blob, or an <see cref="Error"/> when the content was refused.</returns>
    /// <exception cref="InvalidOperationException">The removal scheduled by <see cref="PrepareUpload"/> is not saved yet.</exception>
    /// <exception cref="Exceptions.BlobUploadReclaimedException">The grace period ran out before the content was written, so its removal already started.</exception>
    /// <example>
    /// <code>
    /// var upload = blobStorage.PrepareUpload(context, request);
    /// await context.SaveChangesAsync(cancellationToken); // commits the lease (scheduled removal in case of failure)
    ///
    /// var blob = await blobStorage.AddOnSaveAsync(context, upload.Value, cancellationToken);
    /// invoice.AttachScan(blob.Value.Id);
    /// await context.SaveChangesAsync(cancellationToken); // the row and the aggregate, and cancels the lease (scheduled removal)
    /// </code>
    /// </example>
    Task<IResult<Blob, Error>> AddOnSaveAsync(TContext context, BlobUpload upload, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the catalogue row of a blob.
    /// </summary>
    /// <param name="context">The context holding the catalogue.</param>
    /// <param name="id">The identity to look up.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The blob, or an <see cref="Error"/> when nothing is catalogued under the identity.</returns>
    Task<IResult<Blob, Error>> GetAsync(TContext context, BlobId id, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the content of a blob, with its catalogue row attached.
    /// </summary>
    /// <remarks>
    /// A row whose content is missing from the store fails with <see cref="BlobErrorCodes.BlobContentMissing"/>.
    /// </remarks>
    /// <param name="context">The context holding the catalogue.</param>
    /// <param name="id">The identity to read.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The content and its row, or an <see cref="Error"/>.</returns>
    Task<IResult<BlobContent, Error>> OpenReadAsync(TContext context, BlobId id, CancellationToken cancellationToken);

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
    /// <param name="context">The context to track the removal in. Its save commits the removal.</param>
    /// <param name="blob">The blob to remove.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>A task that completes once the removal is tracked.</returns>
    /// <example>
    /// <code>
    /// context.Invoices.Remove(invoice);
    /// await blobStorage.DeleteOnSaveAsync(context, scan, cancellationToken);
    /// await context.SaveChangesAsync(cancellationToken); // both, or neither
    /// </code>
    /// </example>
    Task DeleteOnSaveAsync(TContext context, Blob blob, CancellationToken cancellationToken);
}
