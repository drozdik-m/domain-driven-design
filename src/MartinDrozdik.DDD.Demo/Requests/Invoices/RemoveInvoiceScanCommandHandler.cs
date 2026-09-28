using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Detaches a scan from its invoice and enqueues the removal of the file.
/// </summary>
/// <param name="context">The context holding the invoices and the catalogue.</param>
/// <param name="blobStorage">Removes the scan.</param>
public class RemoveInvoiceScanCommandHandler(InvoiceDbContext context, IBlobStorage blobStorage)
    : ICommandHandler<RemoveInvoiceScanCommand>
{
    /// <inheritdoc />
    public async Task HandleAsync(RemoveInvoiceScanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await context.Invoices.SingleOrDefaultAsync(i => i.Id == new InvoiceId(command.InvoiceId), cancellationToken)
            ?? throw new BusinessNotFoundException($"No invoice with the id '{command.InvoiceId}'.");

        var removed = invoice.RemoveScan();
        if (removed is null)
        {
            // Nothing to do, and nothing worth complaining about
            return;
        }

        var scan = await blobStorage.GetAsync(removed, cancellationToken);
        if (scan.IsSuccess)
        {
            await blobStorage.DeleteOnSaveAsync(scan.Value, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
