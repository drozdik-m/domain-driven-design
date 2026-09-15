namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// Enqueues outbox messages into the application database context or other atomic-transaction store.
/// </summary>
/// <remarks>
/// <see cref="Add{TMessage}"/> only adds to-be-saved messages, never saves them itself.
/// That save is done using another mechanism, such as <c>DbContext.SaveChangesAsync</c> or a unit-of-work commit.
/// That is the whole point of the pattern: the message is written with the same transaction as the aggregate change that produced it.
/// </remarks>
/// <example>
/// <code>
/// invoice.Issue(recipient);
/// outbox.Add(new SendEmailMessage(recipient.Email, "InvoiceIssued"));
/// await context.SaveChangesAsync(cancellationToken); // both, or neither
/// </code>
/// </example>
public interface IOutbox
{
    /// <summary>
    /// Serializes a message and adds it to the current transaction.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <exception cref="Exceptions.OutboxException">The message type has no registered handler, or its serialized payload exceeds the configured maximum payload length.</exception>
    void Add<TMessage>(TMessage message)
        where TMessage : IOutboxMessage;
}
