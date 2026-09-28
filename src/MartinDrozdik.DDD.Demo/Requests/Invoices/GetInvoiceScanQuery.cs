using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Mediator.Queries;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Opens the scanned document of an invoice for reading.
/// </summary>
/// <param name="InvoiceId">The invoice whose scan to read.</param>
public record GetInvoiceScanQuery(Guid InvoiceId) : IQuery<BlobContent>;
