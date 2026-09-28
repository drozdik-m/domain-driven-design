namespace MartinDrozdik.DDD.Blobs.Exceptions;

/// <summary>
/// Represents an error in the blob storage infrastructure itself.
/// </summary>
public class BlobException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobException"/> class.
    /// </summary>
    public BlobException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BlobException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BlobException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
