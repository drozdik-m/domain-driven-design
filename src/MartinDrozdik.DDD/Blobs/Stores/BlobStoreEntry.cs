namespace MartinDrozdik.DDD.Blobs.Stores;

/// <summary>
/// What an <see cref="IBlobStore"/> knows about one blob by itself.
/// </summary>
/// <remarks>
/// The media type, original name and everything else is stored in the catalogue, not in the store.
/// </remarks>
/// <param name="Key">The address of the blob.</param>
/// <param name="Size">The size of the content, in bytes.</param>
/// <param name="WrittenAt">When the content was written.</param>
public sealed record BlobStoreEntry(BlobKey Key, long Size, DateTimeOffset WrittenAt);
