using MartinDrozdik.DDD.Identities;
using MartinDrozdik.DDD.Identities.Primitive;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// The identity of a stored blob.
/// </summary>
/// <remarks>
/// GUIDv7.
/// </remarks>
/// <param name="key">The actual value of the identifier.</param>
public sealed class BlobId(Guid key) : GuidIdentity<BlobId>(key), IWithImplicitIdentity<BlobId, Guid>
{
    /// <summary>
    /// An implicit casting from <see cref="Guid"/> to <see cref="BlobId"/>.
    /// </summary>
    /// <param name="id">The <i>raw</i> id to be converted.</param>
    public static implicit operator BlobId(Guid id) => new(id);

    /// <summary>
    /// Creates a new identity for a blob that is about to be stored.
    /// </summary>
    /// <returns>A new <see cref="BlobId"/>.</returns>
    public static BlobId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Key.ToString("D");
}
