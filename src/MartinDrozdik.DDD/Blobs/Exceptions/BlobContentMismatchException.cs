using MartinDrozdik.DDD.Blobs.Signatures;
using MartinDrozdik.DDD.Blobs.Stores;

namespace MartinDrozdik.DDD.Blobs.Exceptions;

/// <summary>
/// Reports that content did not start with any of the signatures it had to match.
/// </summary>
/// <remarks>
/// Thrown by <see cref="BlobContentCopier"/> before anything is written.
/// </remarks>
public class BlobContentMismatchException : BlobException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContentMismatchException"/> class.
    /// </summary>
    public BlobContentMismatchException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContentMismatchException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BlobContentMismatchException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContentMismatchException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BlobContentMismatchException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContentMismatchException"/> class.
    /// </summary>
    /// <param name="signatures">The signatures the content had to match, one of them at least.</param>
    /// <returns>The exception.</returns>
    public static BlobContentMismatchException ForSignatures(IEnumerable<ContentSignature> signatures)
    {
        ArgumentNullException.ThrowIfNull(signatures);

        var expected = string.Join(", ", signatures.Select(s => $"'{s}'"));
        return new(expected.Length == 0
            ? "The content was refused, because no signature was allowed."
            : $"The content does not start with any of {expected}.");
    }
}
