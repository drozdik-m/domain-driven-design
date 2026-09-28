using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Blobs.Errors;

/// <summary>
/// The error codes blob storage reports.
/// </summary>
public static class BlobErrorCodes
{
    /// <summary>
    /// Gets the code reporting that no blob is catalogued under the given identity.
    /// </summary>
    public static ErrorCode BlobNotFound { get; } = new ErrorCode(nameof(BlobNotFound));

    /// <summary>
    /// Gets the code reporting that a blob is catalogued but its content is missing from the store.
    /// </summary>
    public static ErrorCode BlobContentMissing { get; } = new ErrorCode(nameof(BlobContentMissing));

    /// <summary>
    /// Gets the code reporting that content is larger than the configured limit.
    /// </summary>
    public static ErrorCode BlobTooLarge { get; } = new ErrorCode(nameof(BlobTooLarge));

    /// <summary>
    /// Gets the code reporting that a media type or extension is not among the allowed ones.
    /// </summary>
    public static ErrorCode BlobTypeNotAllowed { get; } = new ErrorCode(nameof(BlobTypeNotAllowed));
}
