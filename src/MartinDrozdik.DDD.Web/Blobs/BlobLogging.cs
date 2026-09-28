using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// The log messages of blob storage.
/// </summary>
internal static partial class BlobLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Blob storage found nothing to delete at {BlobKey}. It was already gone, which is a success.")]
    internal static partial void LogAlreadyDeleted(ILogger logger, string blobKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blob {BlobKey} is catalogued but its content is not in the store. The row was left alone in case the store is only unreachable.")]
    internal static partial void LogContentMissing(ILogger logger, string blobKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Blob storage swept {ExpiredCount} expired blobs; the outbox removes their content.")]
    internal static partial void LogSwept(ILogger logger, int expiredCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blob storage abandoned the upload to {BlobKey}: it outlasted the grace period and its content is being removed.")]
    internal static partial void LogUploadReclaimed(ILogger logger, string blobKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Blob storage let go of {Count} expired blobs someone else removed or changed first; any still expired are swept again later.")]
    internal static partial void LogExpiredAlreadyRemoved(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blob storage is behind on sweeping: {MaxSweepPasses} passes of {SweepBatchSize} blobs did not clear the expired backlog. The next sweep continues.")]
    internal static partial void LogSweepBehind(ILogger logger, int maxSweepPasses, int sweepBatchSize);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blob storage abandoned a partial write of {BlobKey} and cleaned up after it.")]
    internal static partial void LogWriteAbandoned(ILogger logger, Exception exception, string blobKey);
}
