using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Mediator.Commands;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Attaches a scanned document to an invoice.
/// </summary>
/// <param name="InvoiceId">The invoice to attach the scan to.</param>
/// <param name="Content">The content of the scan.</param>
/// <param name="FileName">The name the file arrived with.</param>
/// <param name="ContentType">The media type the file was declared as, unvalidated.</param>
public record AttachInvoiceScanCommand(Guid InvoiceId, Stream Content, string FileName, string ContentType)
    : ICommand<BlobId>;
