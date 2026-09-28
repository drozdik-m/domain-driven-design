using MartinDrozdik.DDD.Mediator.Commands;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Detaches the scanned document of an invoice and removes the file behind it.
/// </summary>
/// <param name="InvoiceId">The invoice to remove the scan from.</param>
public record RemoveInvoiceScanCommand(Guid InvoiceId) : ICommand;
