namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// Enqueues outbox messages into the application database context or other atomic-transaction store.
/// </summary>
/// <remarks>
/// <para>
/// Every operation is only tracked in the context you pass in.
/// That is the whole point of the pattern: the message is written with the same transaction as the aggregate change that produced it.
/// </para>
/// <para>
/// <typeparamref name="TContext"/> picks the outbox of that context - every context has an outbox of its own, with its own message types and options.
/// It can be Entity Framework Context, a Dapper connection, or any other atomic-transaction store.
/// An implementation registers it for exactly the context final class, never for a base class or interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class IssueInvoiceCommandHandler(InvoiceDbContext context, IOutbox&lt;InvoiceDbContext&gt; outbox)
/// {
///     // ...
///     invoice.Issue(recipient);
///     outbox.AddOnSave(context, new SendEmailMessage(recipient.Email, "InvoiceIssued"));
///     await context.SaveChangesAsync(cancellationToken); // both, or neither
/// }
/// </code>
/// </example>
/// <typeparam name="TContext">The context the messages are saved with.</typeparam>
public interface IOutbox<TContext>
{
    /// <summary>
    /// Serializes a message and adds it to the current transaction, to be delivered as soon as it is committed.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="context">The context to track the message in. Its save commits the message.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <returns>The ticket of the message, e.g. to take it back out with <see cref="RemoveOnSaveAsync"/> before saving.</returns>
    /// <exception cref="Exceptions.OutboxException">The message type is not registered with this outbox, its key is registered by another type, or its serialized payload exceeds the configured maximum payload length.</exception>
    OutboxTicket AddOnSave<TMessage>(TContext context, TMessage message)
        where TMessage : IOutboxMessage;

    /// <summary>
    /// Serializes a message and adds it to the current transaction.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="context">The context to track the message in. Its save commits the message.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="settings">How the message is enqueued.</param>
    /// <returns>The ticket of the message, e.g. to take it back out with <see cref="RemoveOnSaveAsync"/> before saving.</returns>
    /// <exception cref="Exceptions.OutboxException">The message type is not registered with this outbox, its key is registered by another type, or its serialized payload exceeds the configured maximum payload length.</exception>
    OutboxTicket AddOnSave<TMessage>(TContext context, TMessage message, OutboxMessageSettings settings)
        where TMessage : IOutboxMessage;

    /// <summary>
    /// Takes a message back out, as part of the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the message to track its removal. The caller's save may still fail on <i>DbUpdateConcurrencyException</i>
    /// when a processor takes the message between this call and that save.
    /// </para>
    /// </remarks>
    /// <param name="context">The context to track the removal in. Its save commits the removal.</param>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when the removal is tracked, false when <paramref name="context"/> does not find the message, or finds it changed since the ticket was issued (e.g. a processor has claimed it).</returns>
    Task<bool> RemoveOnSaveAsync(TContext context, OutboxTicket ticket, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the delivery of a message to another instant, as part of the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The new instant replaces the old one and may be earlier: an instant in the past makes the message due at once.
    /// </para>
    /// <para>
    /// Loads the message to track its postponement. The caller's save may still fail on <i>DbUpdateConcurrencyException</i>
    /// when a processor takes the message between this call and that save.
    /// </para>
    /// </remarks>
    /// <param name="context">The context to track the postponement in. Its save commits the postponement.</param>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="availableAt">The new earliest instant the message may be delivered.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The new ticket, valid once the caller's transaction commits, or null when <paramref name="context"/> does not find the message, finds it changed since the ticket was issued (e.g. a processor has claimed it), or finds it already delivered or dead-lettered.</returns>
    Task<OutboxTicket?> PostponeOnSaveAsync(TContext context, OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken);
}
