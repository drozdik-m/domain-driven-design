using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Demo.Outbox;

/// <summary>
/// Announces that an invoice draft was created, so somebody can be told about it.
/// </summary>
/// <remarks>
/// <b>It has to make sense on its own.</b>
/// Note the shape: a plain serializable record carrying only what the handler needs.
/// The message is stored as JSON and may be delivered minutes later, on another machine, long after the <see cref="Models.Aggregates.Invoice"/> has moved on.
/// </remarks>
/// <param name="InvoiceId">Identity of the drafted invoice.</param>
/// <param name="InvoiceNumber">Human readable number of the drafted invoice.</param>
/// <param name="RecipientName">Who the invoice is for.</param>
public sealed record InvoiceDraftedMessage(Guid InvoiceId, string InvoiceNumber, string RecipientName) : IOutboxMessage
{
    /// <inheritdoc />
    /// <remarks>
    /// A deliberate, storage-facing key.
    /// Never change it if messages are already in the outbox table, or they will be undeliverable.
    /// </remarks>
    public static OutboxMessageType MessageType => "invoice.drafted.v1";
}
