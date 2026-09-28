namespace MartinDrozdik.DDD.Blobs.Exceptions;

/// <summary>
/// Reports that an upload took longer than the grace period, so its content is being removed as an orphan.
/// </summary>
public class BlobUploadReclaimedException : BlobException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobUploadReclaimedException"/> class.
    /// </summary>
    public BlobUploadReclaimedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobUploadReclaimedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BlobUploadReclaimedException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobUploadReclaimedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BlobUploadReclaimedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobUploadReclaimedException"/> class.
    /// </summary>
    /// <param name="key">The address of the upload.</param>
    /// <returns>The exception.</returns>
    public static BlobUploadReclaimedException For(BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return new($"The upload to '{key.Path}' outlasted the grace period and its content is being removed. Upload it again.");
    }
}
