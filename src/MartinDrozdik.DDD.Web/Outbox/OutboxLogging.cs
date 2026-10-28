using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Source-generated log messages emitted while delivering outbox messages.
/// </summary>
internal static partial class OutboxLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox of {Outbox} delivered message {MessageId} of type {MessageType} on attempt {Attempts}.")]
    internal static partial void LogDispatched(ILogger logger, string outbox, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox of {Outbox}: message {MessageId} of type {MessageType} failed on attempt {Attempts} and will be retried in {Delay}.")]
    internal static partial void LogRetrying(ILogger logger, Exception exception, string outbox, Guid messageId, string messageType, int attempts, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox of {Outbox}: message {MessageId} of type {MessageType} failed on attempt {Attempts} and was dead-lettered. It will not be delivered again.")]
    internal static partial void LogDeadLettered(ILogger logger, Exception exception, string outbox, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox of {Outbox}: attempt {Attempts} of message {MessageId} of type {MessageType} threw after its run was cancelled. It is treated as cut short, not as failed: no outcome is recorded and its lease expires on its own.")]
    internal static partial void LogDeliveryInterrupted(ILogger logger, Exception exception, string outbox, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox of {Outbox}: message {MessageId} of type {MessageType} recorded no outcome of attempt {Attempts} before its lease expired and was dead-lettered. It will not be delivered again.")]
    internal static partial void LogAbandonedDeadLettered(ILogger logger, string outbox, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox of {Outbox}: message {MessageId} could not be read and was dead-lettered. It will not be delivered again.")]
    internal static partial void LogUnreadableDeadLettered(ILogger logger, Exception exception, string outbox, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox of {Outbox} delivered message {MessageId} of type {MessageType} after its lease of {LeaseDuration} expired, and another processor had taken it meanwhile. The delivery is recorded anyway, the message may have been delivered more than once. Raise the lease duration above the longest delivery.")]
    internal static partial void LogDeliveredAfterLeaseExpired(ILogger logger, string outbox, Guid messageId, string messageType, TimeSpan leaseDuration);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox of {Outbox}: message {MessageId} was claimed by another processor and is skipped.")]
    internal static partial void LogClaimLost(ILogger logger, string outbox, Guid messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox of {Outbox} delivered {DispatchedCount} of {ReservedCount} reserved messages in this batch.")]
    internal static partial void LogBatchCompleted(ILogger logger, string outbox, int dispatchedCount, int reservedCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox of {Outbox}: the reservation of message {MessageId} expired before its delivery started. It is skipped and left to other processors.")]
    internal static partial void LogReservationExpired(ILogger logger, string outbox, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox of {Outbox} could not release the unstarted reservations of run {ClaimId}. They expire on their own.")]
    internal static partial void LogReservationsNotReleased(ILogger logger, Exception exception, string outbox, Guid claimId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox of {Outbox} deleted {DeletedCount} delivered messages older than {Retention}.")]
    internal static partial void LogRetentionApplied(ILogger logger, string outbox, int deletedCount, TimeSpan retention);
}
