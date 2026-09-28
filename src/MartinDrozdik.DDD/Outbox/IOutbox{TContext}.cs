namespace MartinDrozdik.DDD.Outbox;

#pragma warning disable S2326 // Unused type parameters should be removed - it ties the outbox to the context whose save commits it - forces devs to check
/// <summary>
/// Enqueues outbox messages into the application database context or other atomic-transaction store.
/// </summary>
/// <remarks>
/// <para>
/// Every operation comes in two flavours.
/// <list type="bullet">
///     <item><c>*OnSave</c> - tracked, saved with the caller's transaction.</item>
///     <item><c>*NowAsync</c> - committed immediately, in a unit of work of their own.</item>
/// </list>
/// </para>
/// <para>
/// You usually use the <c>*OnSave</c>. That is the whole point of the pattern: the message is written with the same transaction as the aggregate change that produced it.
/// </para>
/// <para>
/// The <c>*NowAsync</c> ones committed immidiately, in a unit of work of their own, never part of the caller's transaction.
/// It is how something that has to be undone when a transaction does <i>not</i> commit gets undone - e.g. a file written before the row that claims it.
/// </para>
/// <para>
/// <typeparamref name="TContext"/> names the context the <c>*OnSave</c> operations are tracked in.
/// It can be Entity Framework Context, a Dapper connection, or any other atomic-transaction store.
/// The outbox does not use it for anything else, so it can be a base class or interface rather than the concrete type.
/// This is to force developers to check that the outbox is used with the right context, and not with some other context that happens to be in scope.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class IssueInvoiceCommandHandler(InvoiceDbContext context, IOutbox&lt;InvoiceDbContext&gt; outbox)
/// {
///     // ...
///     invoice.Issue(recipient);
///     outbox.AddOnSave(new SendEmailMessage(recipient.Email, "InvoiceIssued"));
///     await context.SaveChangesAsync(cancellationToken); // both, or neither
/// }
/// </code>
/// </example>
/// <typeparam name="TContext">The context the messages are saved with.</typeparam>
public interface IOutbox<TContext>
{
    /// <summary>
    /// Serializes a message and adds it to the current transaction.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="settings">How the message is enqueued, or null for <see cref="OutboxMessageSettings.Default"/>.</param>
    /// <returns>The ticket of the message, e.g. to take it back out with <see cref="RemoveOnSaveAsync"/> before saving.</returns>
    /// <exception cref="Exceptions.OutboxException">The message type has no registered handler, or its serialized payload exceeds the configured maximum payload length.</exception>
    OutboxTicket AddOnSave<TMessage>(TMessage message, OutboxMessageSettings? settings = null)
        where TMessage : IOutboxMessage;

    /// <summary>
    /// Serializes a message and commits it immediately, in a unit of work of its own.
    /// </summary>
    /// <remarks>
    /// Never part of the caller's transaction.
    /// </remarks>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="settings">How the message is enqueued, or null for <see cref="OutboxMessageSettings.Default"/>.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The ticket of the message.</returns>
    /// <exception cref="Exceptions.OutboxException">The message type has no registered handler, or its serialized payload exceeds the configured maximum payload length.</exception>
    Task<OutboxTicket> AddNowAsync<TMessage>(TMessage message, OutboxMessageSettings? settings, CancellationToken cancellationToken)
        where TMessage : IOutboxMessage;

    /// <summary>
    /// Takes a message back out, as part of the current transaction.
    /// </summary>
    /// <remarks>
    /// Loads the message to track its removal. The caller's save may still fail on <i>DbUpdateConcurrencyException</i>
    /// when a processor takes the message between this call and that save.
    /// </remarks>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when the removal is tracked, false when the message changed since the ticket was issued - a processor has taken it.</returns>
    Task<bool> RemoveOnSaveAsync(OutboxTicket ticket, CancellationToken cancellationToken);

    /// <summary>
    /// Takes a message back out, and commits that immediately in a unit of work of its own.
    /// </summary>
    /// <remarks>
    /// May fail on <i>DbUpdateConcurrencyException</i> when a processor has taken the message since the message was issued.
    /// </remarks>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>True when the message was removed, false when it changed since the ticket was issued - a processor has taken it.</returns>
    Task<bool> RemoveNowAsync(OutboxTicket ticket, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the delivery of a message later, as part of the current transaction.
    /// </summary>
    /// <remarks>
    /// Loads the message to track its postponement. The caller's save may still fail on <i>DbUpdateConcurrencyException</i>
    /// when a processor takes the message between this call and that save.
    /// </remarks>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="availableAt">The new earliest instant the message may be delivered.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The new ticket, valid once the caller's transaction commits, or null when the message changed since the ticket was issued - a processor has taken it.</returns>
    Task<OutboxTicket?> PostponeOnSaveAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the delivery of a message later, and commits that immediately in a unit of work of its own.
    /// </summary>
    /// <remarks>
    /// May fail on <i>DbUpdateConcurrencyException</i> when a processor has taken the message since the message was issued.
    /// </remarks>
    /// <param name="ticket">The ticket of the message, as last issued.</param>
    /// <param name="availableAt">The new earliest instant the message may be delivered.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The new ticket, or null when the message changed since the ticket was issued - a processor has taken it.</returns>
    Task<OutboxTicket?> PostponeNowAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken);
}
#pragma warning restore S2326 // Unused type parameters should be removed
