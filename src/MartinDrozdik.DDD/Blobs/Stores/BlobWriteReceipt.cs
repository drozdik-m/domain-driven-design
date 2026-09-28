using MartinDrozdik.DDD.Blobs.Checksums;

namespace MartinDrozdik.DDD.Blobs.Stores;

/// <summary>
/// What an <see cref="IBlobStore"/> measured while writing a blob.
/// </summary>
/// <remarks>
/// The size and checksum are observed during the write.
/// </remarks>
/// <param name="Size">The number of bytes written.</param>
/// <param name="Checksum">The hash of what was written, or null when hashing was turned off.</param>
/// <param name="WrittenAt">When the write completed.</param>
public sealed record BlobWriteReceipt(long Size, BlobChecksum? Checksum, DateTimeOffset WrittenAt);
