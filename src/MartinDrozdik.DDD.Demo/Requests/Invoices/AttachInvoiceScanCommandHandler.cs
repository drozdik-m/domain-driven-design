using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// Stores a scan and attaches it to its invoice, in one transaction.
/// </summary>
/// <remarks>
/// <b>Everything here is one save.</b>
/// The only thing that happens outside the transaction is the write of the content itself, which is ok,
/// because a rollback leaves behind a cleanup outbox message.
/// </remarks>
/// <param name="context">The context holding the invoices and the catalogue.</param>
/// <param name="blobStorage">Stores the scan.</param>
public class AttachInvoiceScanCommandHandler(InvoiceDbContext context, IBlobStorage blobStorage)
    : ICommandHandler<AttachInvoiceScanCommand, BlobId>
{
    /// <inheritdoc />
    public async Task<BlobId> HandleAsync(AttachInvoiceScanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Declared by the client, so it is validated here rather than trusted
        var contentType = MediaType.Create(command.ContentType);
        if (contentType.IsFailure)
        {
            throw contentType.Error.ToBusinessRuleException();
        }

        var invoice = await context.Invoices.SingleOrDefaultAsync(i => i.Id == new InvoiceId(command.InvoiceId), cancellationToken)
            ?? throw new BusinessNotFoundException($"No invoice with the id '{command.InvoiceId}'.");

        var result = await blobStorage.AddOnSaveAsync(
            new BlobUploadRequest
            {
                Container = BlobContainers.InvoiceScans,
                Content = command.Content,
                OriginalFileName = command.FileName,
                ContentType = contentType.Value,
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new BusinessRuleValidationException(result.Error.Message);
        }

        var replaced = invoice.AttachScan(result.Value.Id);
        if (replaced is not null)
        {
            // A blob is immutable, so replacing one means storing the new and enqueueing the old for removal
            var previous = await blobStorage.GetAsync(replaced, cancellationToken);
            if (previous.IsSuccess)
            {
                await blobStorage.DeleteOnSaveAsync(previous.Value, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
