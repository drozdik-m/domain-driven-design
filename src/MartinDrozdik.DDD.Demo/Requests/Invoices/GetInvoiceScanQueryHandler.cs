using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Opens the scan of an invoice.
/// </summary>
/// <param name="context">The context holding the invoices.</param>
/// <param name="blobStorage">Reads the scan.</param>
public class GetInvoiceScanQueryHandler(InvoiceDbContext context, IBlobStorage blobStorage)
    : IQueryHandler<GetInvoiceScanQuery, BlobContent>
{
    /// <inheritdoc />
    public async Task<BlobContent> HandleAsync(GetInvoiceScanQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await context.Invoices.AsNoTracking().SingleOrDefaultAsync(i => i.Id == new InvoiceId(query.InvoiceId), cancellationToken)
            ?? throw new BusinessNotFoundException($"No invoice with the id '{query.InvoiceId}'.");

        if (invoice.ScanId is not { } scanId)
        {
            throw new BusinessNotFoundException($"The invoice '{query.InvoiceId}' has no scan attached.");
        }

        var result = await blobStorage.OpenReadAsync(scanId, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new BusinessNotFoundException(result.Error.Message);
        }

        return result.Value;
    }
}
