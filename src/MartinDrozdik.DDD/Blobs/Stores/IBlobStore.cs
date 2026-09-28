namespace MartinDrozdik.DDD.Blobs.Stores;

/// <summary>
/// Reads and writes the bytes of blobs.
/// F.e. handles the file system, a cloud bucket, or a database table with a binary column.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about the catalogue, a database or a transaction.
/// <b>Absence is never an error</b> to make deletions idempotent.
/// </remarks>
public interface IBlobStore
{
    /// <summary>
    /// Writes the content of a blob.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads <paramref name="content"/> exactly once, forward only, non-seekable stream works.
    /// </para>
    /// <para>
    /// An address that is already taken is refused.
    /// It's recommended to schedule the removal of the content first, so that a failed write/transaction is removed - <see cref="IBlobStorage"/> does it through the outbox.
    /// </para>
    /// </remarks>
    /// <param name="key">The address to write to.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="options">How to write it.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>What was written.</returns>
    /// <exception cref="Exceptions.BlobException">The address is already taken, the content exceeded the allowed size, or the store failed.</exception>
    Task<BlobWriteReceipt> WriteAsync(BlobKey key, Stream content, BlobWriteOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the content of a blob for reading.
    /// </summary>
    /// <param name="key">The address to read.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The content, which the caller owns and disposes, or null when there is nothing at the address.</returns>
    Task<Stream?> OpenReadAsync(BlobKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Reads what the store knows about a blob without transferring its content.
    /// </summary>
    /// <param name="key">The address to inspect.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The entry, or null when there is nothing at the address.</returns>
    Task<BlobStoreEntry?> GetEntryAsync(BlobKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Decides whether a blob has content in the store.
    /// </summary>
    /// <param name="key">The address to test.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when there is content at the address, else false.</returns>
    Task<bool> ExistsAsync(BlobKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the content of a blob.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// Deleting something that is already gone is a success.
    /// </remarks>
    /// <param name="key">The address to remove.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when content was removed, false when there was nothing to remove.</returns>
    Task<bool> DeleteAsync(BlobKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Copies the content of a blob to another address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The way to "change" a blob, which is otherwise immutable: copy it to a new identity and delete the old one.
    /// </para>
    /// <para>
    /// Behaves like <see cref="WriteAsync"/> towards the destination, so schedule the removal of the destination first to prevent stale files.
    /// </para>
    /// </remarks>
    /// <param name="source">The address to copy from.</param>
    /// <param name="destination">The address to copy to.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when the content was copied, false when there was nothing at <paramref name="source"/>.</returns>
    /// <exception cref="Exceptions.BlobException">The destination is already taken, or the store failed.</exception>
    Task<bool> CopyAsync(BlobKey source, BlobKey destination, CancellationToken cancellationToken);
}
