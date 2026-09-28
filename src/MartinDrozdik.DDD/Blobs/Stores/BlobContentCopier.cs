using System.Buffers;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Exceptions;

namespace MartinDrozdik.DDD.Blobs.Stores;

/// <summary>
/// Copies the content of a blob into a store, measuring it on the way past.
/// </summary>
/// <remarks>
/// <para>The source is read forwards, exactly once.</para>
/// </remarks>
public static class BlobContentCopier
{
    /// <summary>
    /// The number of bytes moved per read.
    /// </summary>
    private const int BufferSize = 81920;

    /// <summary>
    /// Copies content from one stream to another, measuring its size and hashing it as it goes.
    /// </summary>
    /// <param name="source">The content to copy.</param>
    /// <param name="destination">Where to copy it.</param>
    /// <param name="options">How to write it, including whether to hash and how large it may be.</param>
    /// <param name="timeProvider">Tells the time the write completed.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>What was written.</returns>
    /// <exception cref="BlobTooLargeException">The content exceeded <see cref="BlobWriteOptions.MaxSize"/>.</exception>
    public static async Task<BlobWriteReceipt> CopyAsync(
        Stream source,
        Stream destination,
        BlobWriteOptions options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        using var hash = options.ComputeChecksum
            ? ChecksumAlgorithm.Sha256.CreateHash()
            : null;

        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        var size = 0L;

        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                size += read;

                if (options.MaxSize is { } maxSize && size > maxSize)
                {
                    throw BlobTooLargeException.ForLimit(maxSize);
                }

                hash?.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        await destination.FlushAsync(cancellationToken);

        var checksum = hash is null
            ? null
            : BlobChecksum.Sha256(Convert.ToHexStringLower(hash.GetCurrentHash())).Value;

        return new BlobWriteReceipt(size, checksum, timeProvider.GetUtcNow());
    }
}
