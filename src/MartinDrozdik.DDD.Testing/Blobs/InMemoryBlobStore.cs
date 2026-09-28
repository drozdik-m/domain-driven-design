using System.Collections.Concurrent;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;

namespace MartinDrozdik.DDD.Testing.Blobs;

/// <summary>
/// An <see cref="IBlobStore"/> that keeps content in memory.
/// </summary>
/// <remarks>
/// <para>
/// For tests that care about what the application does with blobs rather than about the store/file system.
/// </para>
/// </remarks>
/// <param name="timeProvider">Tells the time content was written, or null to use the system clock.</param>
public sealed class InMemoryBlobStore(TimeProvider? timeProvider = null) : IBlobStore
{
    private readonly ConcurrentDictionary<string, StoredBlob> _blobs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Exception> _failures = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Gets the number of blobs currently held.
    /// </summary>
    public int Count => _blobs.Count;

    /// <summary>
    /// Makes every operation on the content at an address fail, to prove the caller can handle a store that is having a bad day.
    /// </summary>
    /// <param name="key">The address to break.</param>
    /// <param name="exception">The failure to raise.</param>
    public void FailOn(BlobKey key, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(exception);

        _failures[key.Path] = exception;
    }

    /// <summary>
    /// Undoes <see cref="FailOn"/>, so the content at an address works again.
    /// </summary>
    /// <param name="key">The address to mend.</param>
    public void Heal(BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _failures.TryRemove(key.Path, out _);
    }

    /// <summary>
    /// Removes the content of a blob behind the application's back, the way a careless operator would.
    /// </summary>
    /// <remarks>
    /// The catalogue is left alone.
    /// Produces a drift to test these situations.
    /// </remarks>
    /// <param name="key">The address to empty.</param>
    /// <returns>True when content was removed, else false.</returns>
    public bool RemoveSilently(BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _blobs.TryRemove(key.Path, out _);
    }

    /// <summary>
    /// Reads the content of a blob.
    /// </summary>
    /// <param name="key">The address to read.</param>
    /// <param name="content">The content found, or an empty array.</param>
    /// <returns>True when there was content at the address, else false.</returns>
    public bool TryRead(BlobKey key, out byte[] content)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_blobs.TryGetValue(key.Path, out var blob))
        {
            content = blob.Content;
            return true;
        }

        content = [];
        return false;
    }

    /// <inheritdoc />
    public async Task<BlobWriteReceipt> WriteAsync(BlobKey key, Stream content, BlobWriteOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfBroken(key);

        if (_blobs.ContainsKey(key.Path))
        {
            throw new BlobException($"'{key.Path}' is already taken.");
        }

        await using var destination = new MemoryStream();
        var receipt = await BlobContentCopier.CopyAsync(content, destination, options, _timeProvider, cancellationToken);

        _blobs[key.Path] = new StoredBlob(key, destination.ToArray(), receipt.WrittenAt);

        return receipt;
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        ThrowIfBroken(key);

        if (!_blobs.TryGetValue(key.Path, out var blob))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(blob.Content, writable: false));
    }

    /// <inheritdoc />
    public Task<BlobStoreEntry?> GetEntryAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        ThrowIfBroken(key);

        return Task.FromResult(_blobs.TryGetValue(key.Path, out var blob) ? blob.ToEntry() : null);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        ThrowIfBroken(key);

        return Task.FromResult(_blobs.ContainsKey(key.Path));
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(BlobKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        ThrowIfBroken(key);

        return Task.FromResult(_blobs.TryRemove(key.Path, out _));
    }

    /// <inheritdoc />
    public Task<bool> CopyAsync(BlobKey source, BlobKey destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        ThrowIfBroken(source);
        ThrowIfBroken(destination);

        if (!_blobs.TryGetValue(source.Path, out var blob))
        {
            return Task.FromResult(false);
        }

        if (_blobs.ContainsKey(destination.Path))
        {
            throw new BlobException($"'{destination.Path}' is already taken.");
        }

        _blobs[destination.Path] = new StoredBlob(destination, blob.Content, _timeProvider.GetUtcNow());

        return Task.FromResult(true);
    }

    /// <summary>
    /// Raises the failure an address was broken with, if it was.
    /// </summary>
    /// <param name="key">The address being operated on.</param>
    private void ThrowIfBroken(BlobKey key)
    {
        if (_failures.TryGetValue(key.Path, out var exception))
        {
            throw exception;
        }
    }

    /// <summary>
    /// One blob held in memory.
    /// </summary>
    /// <param name="Key">The address of the blob.</param>
    /// <param name="Content">The content of the blob.</param>
    /// <param name="WrittenAt">When the content was written.</param>
    private sealed record StoredBlob(BlobKey Key, byte[] Content, DateTimeOffset WrittenAt)
    {
        /// <summary>
        /// Describes the blob the way a store reports it.
        /// </summary>
        /// <returns>The entry.</returns>
        public BlobStoreEntry ToEntry() => new(Key, Content.Length, WrittenAt);
    }
}
