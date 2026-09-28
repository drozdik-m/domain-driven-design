using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs.Stores;

/// <summary>
/// An <see cref="IBlobStore"/> that keeps content in a folder on disk.
/// </summary>
/// <remarks>
/// <para>
/// Every container is kept in the folder configured via <see cref="FileBlobOptions"/>.
/// Path format: <c>{containerFolder}/{blobId}</c>.
/// A container without a folder is refused.
/// </para>
/// <para>
/// Nothing here locks. Several instances may share each folder.
/// Container folders are never removed, and deleting what is already gone is a success (idempotent).
/// The store is safe to use in a multi-instance environment.
/// </para>
/// </remarks>
/// <param name="fileOptions">Where the content is kept.</param>
/// <param name="timeProvider">Tells the time content was written.</param>
/// <param name="logger">Records what went missing and when.</param>
internal sealed class FileBlobStore(
    IOptions<FileBlobOptions> fileOptions,
    TimeProvider timeProvider,
    ILogger<FileBlobStore> logger) : IBlobStore
{
    private readonly Dictionary<BlobContainer, string> _folders = fileOptions.Value.Containers
        .ToDictionary(c => c.Key, c => Path.GetFullPath(c.Value.Path));

    /// <inheritdoc />
    public async Task<BlobWriteReceipt> WriteAsync(BlobKey key, Stream content, BlobWriteOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        var path = GetFilePath(key);

        // Idempotent, and safe when several instances create the same container at once
        Directory.CreateDirectory(GetContainerPath(key.Container));

        // Cleanup only runs once the file is ours - a failed open must not remove a file f.e. someone else just created.
        var created = false;

        try
        {
            // Create file content - the stream is disposed from the moment it exists.
            // FileShare.None, so nothing reads a partial file.
            BlobWriteReceipt receipt;
            await using (var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                receipt = await BlobContentCopier.CopyAsync(content, destination, options, timeProvider, cancellationToken);
            }

            // Set the last write time after the stream is closed - closing flushes and would move the time again.
            // Stamped with the application's clock, so the entry reports the same instant as the receipt/result.
            File.SetLastWriteTimeUtc(path, receipt.WrittenAt.UtcDateTime);

            return receipt;
        }
        catch (IOException exception) when (!created && File.Exists(path))
        {
            // Should never happen due to unique blob ids
            throw new BlobException($"'{key.Path}' is already taken.", exception);
        }
        catch (Exception exception) when (created)
        {
            // Partial file is left behind - cleanup
            BlobLogging.LogWriteAbandoned(logger, exception, key.Path);
            TryDeleteFile(path);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // FileShare.Delete lets another instance remove the blob while this one is still read.
            // The file descriptor is kept open until the stream is disposed, so the file is not removed while reading.
            return Task.FromResult<Stream?>(
                new FileStream(GetFilePath(key), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, bufferSize: 4096, useAsync: true));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Another instance may delete the file between a check and the open (e.g. the sweep removing an expired blob).
            // Absence is reported, not thrown.
            return Task.FromResult<Stream?>(null);
        }
    }

    /// <inheritdoc />
    public Task<BlobStoreEntry?> GetEntryAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var file = new FileInfo(GetFilePath(key));

        return Task.FromResult(file.Exists
            ? new BlobStoreEntry(key, file.Length, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero))
            : null);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        return Task.FromResult(File.Exists(GetFilePath(key)));
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var path = GetFilePath(key);
        if (!File.Exists(path))
        {
            BlobLogging.LogAlreadyDeleted(logger, key.Path);
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // File.Delete tolerates a missing file but not a missing folder - e.g. the container was removed by hand
            BlobLogging.LogAlreadyDeleted(logger, key.Path);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> CopyAsync(BlobKey source, BlobKey destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        // Use our safe read to avoid a race with another instance
        await using var content = await OpenReadAsync(source, cancellationToken);
        if (content is null)
        {
            return false;
        }

        await WriteAsync(destination, content, new BlobWriteOptions(ComputeChecksum: false), cancellationToken);

        return true;
    }

    /// <summary>
    /// Removes a file, tolerating one that is already gone or still in use.
    /// </summary>
    /// <param name="path">The file to remove.</param>
    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Left for the removal the upload scheduled through the outbox
        }
        catch (UnauthorizedAccessException)
        {
            // As above
        }
    }

    /// <summary>
    /// Gets the file a blob address points to.
    /// </summary>
    /// <param name="key">The address to resolve.</param>
    /// <returns>The full path of the file.</returns>
    private string GetFilePath(BlobKey key) => Path.Combine(GetContainerPath(key.Container), key.Id.ToString());

    /// <summary>
    /// Gets the folder a container is kept in.
    /// </summary>
    /// <param name="container">The container to resolve.</param>
    /// <returns>The full path of the folder.</returns>
    /// <exception cref="BlobException">The container has no folder.</exception>
    private string GetContainerPath(BlobContainer container)
        => _folders.TryGetValue(container, out var folder)
            ? folder
            : throw new BlobException($"Blob container '{container}' has no folder in the file store. Give it one with {nameof(FileBlobStoreConfig)}.{nameof(FileBlobStoreConfig.WithContainer)}.");
}
