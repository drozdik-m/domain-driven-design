using System.Diagnostics;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// The full address of a blob inside a <see cref="IBlobStore"/>.
/// The paths should never collide, because the blob id is unique.
/// Two segments - <c>{container}/{blobId}</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///     <item>the container groups a kind of content,</item>
///     <item>the blob id names the content itself – unique address.</item>
/// </list>
/// Everything else about a blob - its name, type, size - lives in the catalogue.
/// </remarks>
[DebuggerDisplay("{Path}")]
public sealed class BlobKey : ValueObject
{
    /// <summary>
    /// The character separating the segments of a <see cref="Path"/>.
    /// </summary>
    /// <remarks>
    /// A forward slash on every platform. A store translates it to whatever its backend uses.
    /// </remarks>
    public const char Separator = '/';

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobKey"/> class.
    /// </summary>
    /// <param name="container">The container the blob lives in.</param>
    /// <param name="id">The identity of the blob.</param>
    private BlobKey(BlobContainer container, BlobId id)
    {
        Container = container;
        Id = id;
    }

    /// <summary>
    /// Gets the container the blob lives in.
    /// </summary>
    public BlobContainer Container { get; }

    /// <summary>
    /// Gets the identity of the blob.
    /// </summary>
    public BlobId Id { get; }

    /// <summary>
    /// Gets the address as a store-relative path, separated by <see cref="Separator"/> on every platform.
    /// </summary>
    public string Path => $"{Container}{Separator}{Id}";

    /// <summary>
    /// Creates a key from its segments.
    /// </summary>
    /// <param name="container">The container the blob lives in.</param>
    /// <param name="id">The identity of the blob.</param>
    /// <returns>The key.</returns>
    public static BlobKey Create(BlobContainer container, BlobId id)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(id);

        return new BlobKey(container, id);
    }

    /// <inheritdoc />
    public override string ToString() => Path;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Container;
        yield return Id;
    }
}
