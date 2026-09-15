using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Demo.Outbox;

/// <summary>
/// Delivers <see cref="InvoiceDraftedMessage"/>.
/// A real application would send an email here; this one just logs, so the demo has nothing to configure.
/// </summary>
/// <remarks>
/// Here are no try/catch; no bookkeeping.
/// </remarks>
/// <param name="logger">Logger standing in for a real email transport.</param>
public class InvoiceDraftedMessageHandler(ILogger<InvoiceDraftedMessageHandler> logger)
    : IOutboxMessageHandler<InvoiceDraftedMessage>
{
    /// <inheritdoc />
    public Task HandleAsync(InvoiceDraftedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.LogInformation("Telling {RecipientName} about drafted invoice {InvoiceNumber} ({InvoiceId}).", message.RecipientName, message.InvoiceNumber, message.InvoiceId);

        return Task.CompletedTask;
    }
}
