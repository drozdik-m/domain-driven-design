using System.Security.Cryptography;
using MartinDrozdik.DDD.Enumerations;

namespace MartinDrozdik.DDD.Blobs.Checksums;

/// <summary>
/// The algorithm a <see cref="BlobChecksum"/> was produced with.
/// </summary>
public sealed class ChecksumAlgorithm : StaticEnumeration<ChecksumAlgorithm>
{
    /// <summary>
    /// The maximum number of characters of a member's <see cref="Enumeration.Name"/>, which is what gets stored.
    /// </summary>
    public const int NameMaxLength = 32;

    /// <summary>
    /// SHA-256. The only algorithm the library computes.
    /// </summary>
    public static readonly ChecksumAlgorithm Sha256 = new(new EnumerationName("sha256"), HashAlgorithmName.SHA256, SHA256.HashSizeInBytes);

    /// <summary>
    /// Initializes a new instance of the <see cref="ChecksumAlgorithm"/> class.
    /// </summary>
    /// <param name="name">The name written to storage.</param>
    /// <param name="hashAlgorithmName">The .NET algorithm producing the hash.</param>
    /// <param name="hashSizeInBytes">The number of bytes of a hash.</param>
    private ChecksumAlgorithm(EnumerationName name, HashAlgorithmName hashAlgorithmName, int hashSizeInBytes)
        : base(name)
    {
        HashAlgorithmName = hashAlgorithmName;
        HashLength = hashSizeInBytes * 2;
    }

    /// <summary>
    /// Gets the .NET algorithm producing the hash.
    /// </summary>
    public HashAlgorithmName HashAlgorithmName { get; }

    /// <summary>
    /// Gets the number of hexadecimal characters of a hash.
    /// </summary>
    public int HashLength { get; }

    /// <summary>
    /// Starts a hash that content can be appended to while it streams.
    /// </summary>
    /// <returns>A new incremental hash, owned by the caller.</returns>
    public IncrementalHash CreateHash() => IncrementalHash.CreateHash(HashAlgorithmName);
}
