namespace MartinDrozdik.DDD.Blobs.Stores;

/// <summary>
/// How to write a blob.
/// </summary>
/// <param name="ComputeChecksum">
/// Whether to hash the content while writing it.
/// Hash is computed in the same pass as writing (cheap and convenient).
/// </param>
/// <param name="MaxSize">
/// The most bytes that may be written, or null for no limit.
/// Enforced during writing.
/// Oversized content is refused.
/// </param>
public sealed record BlobWriteOptions(bool ComputeChecksum = true, long? MaxSize = null)
{
    /// <summary>
    /// Writes with hashing and no size limit.
    /// </summary>
    public static readonly BlobWriteOptions Default = new();
}
