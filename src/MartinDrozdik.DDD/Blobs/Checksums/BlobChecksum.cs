using System.Diagnostics;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs.Checksums;

/// <summary>
/// The content hash of a blob.
/// </summary>
/// <remarks>
/// Computed while the content streams into the store, in the same pass that writes it – efficient.
/// </remarks>
[DebuggerDisplay("{Algorithm}:{Value}")]
public sealed class BlobChecksum : ValueObject
{
    /// <summary>
    /// The maximum number of characters of a <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    /// Long enough for a SHA-512 hash, the longest the library could grow to produce.
    /// </remarks>
    public const int MaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobChecksum"/> class.
    /// </summary>
    /// <param name="algorithm">The algorithm the hash was produced with.</param>
    /// <param name="value">The already validated hash, lowercase hexadecimal.</param>
    private BlobChecksum(ChecksumAlgorithm algorithm, string value)
    {
        Algorithm = algorithm;
        Value = value;
    }

    /// <summary>
    /// Gets the algorithm the hash was produced with.
    /// </summary>
    public ChecksumAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the hash, lowercase hexadecimal.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a checksum from a hash.
    /// </summary>
    /// <param name="algorithm">The algorithm the hash was produced with.</param>
    /// <param name="value">The hash, hexadecimal in either case.</param>
    /// <returns>The checksum, or an <see cref="ErrorCodes.InvalidObject"/> error when the hash is empty, not hexadecimal, or the wrong length for the algorithm.</returns>
    public static Result<BlobChecksum, Error> Create(ChecksumAlgorithm algorithm, string? value)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        if (Validate(algorithm, value) is { } violation)
        {
            return BlobErrors.Invalid(nameof(Value), violation);
        }

        return new BlobChecksum(algorithm, value?.ToLowerInvariant() ?? string.Empty);
    }

    /// <summary>
    /// Creates a SHA-256 checksum.
    /// </summary>
    /// <param name="value">The hash, hexadecimal in either case.</param>
    /// <returns>The checksum, or an <see cref="ErrorCodes.InvalidObject"/> error when the hash is not a SHA-256 one.</returns>
    public static Result<BlobChecksum, Error> Sha256(string? value) => Create(ChecksumAlgorithm.Sha256, value);

    /// <summary>
    /// Renders the checksum as a <c>strong HTTP ETag</c>.
    /// </summary>
    /// <remarks>
    /// Strong because blobs are immutable.
    /// </remarks>
    /// <returns>The quoted entity tag.</returns>
    public string ToETag() => $"\"{Value}\"";

    /// <inheritdoc />
    public override string ToString() => $"{Algorithm}:{Value}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Algorithm;
        yield return Value;
    }

    /// <summary>
    /// Checks a hash against the rules of an algorithm.
    /// </summary>
    /// <param name="algorithm">The algorithm the hash was produced with.</param>
    /// <param name="value">The hash to check.</param>
    /// <returns>The description of the violated rule, or null when the hash is valid.</returns>
    private static string? Validate(ChecksumAlgorithm algorithm, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "A checksum must not be empty.";
        }

        if (!value.All(char.IsAsciiHexDigit))
        {
            return $"A checksum must be hexadecimal, but '{value}' is not.";
        }

        if (value.Length != algorithm.HashLength)
        {
            return $"A {algorithm} checksum is {algorithm.HashLength} characters long, but '{value}' is {value.Length}.";
        }

        return null;
    }
}
