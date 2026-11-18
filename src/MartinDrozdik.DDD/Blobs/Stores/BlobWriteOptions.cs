using MartinDrozdik.DDD.Blobs.Signatures;

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
/// <param name="ContentSignatures">
/// The signatures the content has to start with, one of them at least, or null to write it unchecked.
/// Checked on the first read, before anything is written; an empty list refuses all content.
/// </param>
public sealed record BlobWriteOptions(bool ComputeChecksum = true, long? MaxSize = null, IReadOnlyCollection<ContentSignature>? ContentSignatures = null)
{
    /// <summary>
    /// Writes with hashing and no size limit.
    /// </summary>
    public static readonly BlobWriteOptions Default = new();
}
