using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Source-generated log messages emitted while delivering outbox messages.
/// </summary>
internal static partial class OutboxLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox delivered message {MessageId} of type {MessageType} on attempt {Attempts}.")]
    internal static partial void LogDispatched(ILogger logger, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} of type {MessageType} failed on attempt {Attempts} and will be retried in {Delay}.")]
    internal static partial void LogRetrying(ILogger logger, Exception exception, Guid messageId, string messageType, int attempts, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {MessageId} of type {MessageType} failed on attempt {Attempts} and was dead-lettered. It will not be delivered again.")]
    internal static partial void LogDeadLettered(ILogger logger, Exception exception, Guid messageId, string messageType, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} carries the message type {MessageType}, which has no registered handler. It is retried in case a deployment is still rolling out, and dead-lettered once the retries run out.")]
    internal static partial void LogUnknownMessageType(ILogger logger, Guid messageId, string messageType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox message {MessageId} was claimed by another processor and is skipped.")]
    internal static partial void LogClaimLost(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox delivered {DispatchedCount} of {ClaimedCount} messages in this batch.")]
    internal static partial void LogBatchCompleted(ILogger logger, int dispatchedCount, int claimedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox deleted {DeletedCount} delivered messages older than {Retention}.")]
    internal static partial void LogRetentionApplied(ILogger logger, int deletedCount, TimeSpan retention);
}
