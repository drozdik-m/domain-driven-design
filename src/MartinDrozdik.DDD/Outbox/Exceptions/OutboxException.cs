namespace MartinDrozdik.DDD.Outbox.Exceptions;

/// <summary>
/// Represents an error in the outbox infrastructure itself, such as:
/// <list type="bullet">
///     <item>a duplicate message type registration</item>
///     <item>a message type with no registered handler</item>
///     <item>a payload that cannot be stored</item>
///     <item>a payload that cannot be deserialized</item>
/// </list>
/// </summary>
/// <remarks>
/// A failure raised by a message handler is <b>not</b> an <see cref="OutboxException"/>.
/// Handler exceptions are retried according to the configured backoff and eventually dead-lettered.
/// </remarks>
public class OutboxException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxException"/> class.
    /// </summary>
    public OutboxException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public OutboxException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public OutboxException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
