namespace MartinDrozdik.DDD.Blobs.Exceptions;

/// <summary>
/// Reports that content was larger than the configured limit.
/// </summary>
public class BlobTooLargeException : BlobException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTooLargeException"/> class.
    /// </summary>
    public BlobTooLargeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTooLargeException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BlobTooLargeException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTooLargeException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BlobTooLargeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the largest allowed size, in bytes, or null when the limit is not known.
    /// </summary>
    public long? MaxSize { get; private init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTooLargeException"/> class.
    /// </summary>
    /// <param name="maxSize">The largest allowed size, in bytes.</param>
    /// <returns>The exception.</returns>
    public static BlobTooLargeException ForLimit(long maxSize)
        => new($"The content is larger than the allowed {maxSize} bytes.") { MaxSize = maxSize };
}
