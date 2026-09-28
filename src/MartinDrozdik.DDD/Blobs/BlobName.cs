using System.Diagnostics;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// A sanitized copy of the name a blob arrived with, kept in the catalogue.
/// </summary>
/// <remarks>
/// Not part of the blob's address - servers to be pleasant to read and to offer as a download name.
/// </remarks>
[DebuggerDisplay("{Value}")]
public sealed class BlobName : ValueObject
{
    /// <summary>
    /// The maximum number of characters of a <see cref="Value"/>.
    /// </summary>
    public const int MaxLength = PathExtensions.MaxStoredFileNameLength;

    /// <summary>
    /// The name used when nothing usable is left of the original.
    /// </summary>
    public const string Fallback = PathExtensions.StoredFileNameFallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobName"/> class.
    /// </summary>
    /// <param name="value">The sanitized or stored name.</param>
    private BlobName(string value)
    {
        Value = value;

        var extension = Path.GetExtension(value).TrimStart('.');
        Extension = extension.Length == 0 ? null : extension.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the extension of the name, lowercase and without the leading dot, or null when the name has none.
    /// </summary>
    public string? Extension { get; }

    /// <summary>
    /// Creates a name from the one a file arrived with.
    /// </summary>
    /// <remarks>
    /// Sanitized by <see cref="PathExtensions.ToStoredFileName(string)"/>, so the result is always a single path segment that is safe to create on disk or offer as a download name.
    /// </remarks>
    /// <param name="fileName">The name the file arrived with.</param>
    /// <returns>The sanitized name, or <see cref="Fallback"/> when nothing usable is left.</returns>
    public static BlobName FromFileName(string? fileName)
    {
        return new((fileName ?? string.Empty).ToStoredFileName());
    }

    /// <summary>
    /// Reads back a name that was stored before, as it is.
    /// </summary>
    /// <param name="value">The name as it was stored.</param>
    /// <returns>The name.</returns>
    public static BlobName FromStored(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        return new BlobName(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
