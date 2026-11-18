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
/// Stores a scan and attaches it to its invoice.
/// </summary>
/// <remarks>
/// <b>An upload takes two saves.</b>
/// The first one stores an outbox message that removes the content later if upload fails.
/// The second one stores the blob and the invoice and cancels the fallback removal.
/// </remarks>
/// <param name="context">The context holding the invoices and the catalogue.</param>
/// <param name="blobStorage">Stores the scan.</param>
public class AttachInvoiceScanCommandHandler(InvoiceDbContext context, IBlobStorage<InvoiceDbContext> blobStorage)
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

        // Step 1: schedule the removal of the content and save it before anything is written
        var upload = blobStorage.PrepareUpload(
            context,
            new BlobUploadRequest
            {
                Container = BlobContainers.InvoiceScans,
                Content = command.Content,
                OriginalFileName = command.FileName,
                ContentType = contentType.Value,
            });

        if (!upload.IsSuccess)
        {
            throw new BusinessRuleValidationException(upload.Error.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Step 2: write the content and track the blob, which the save below commits with the invoice
        var result = await blobStorage.AddOnSaveAsync(context, upload.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new BusinessRuleValidationException(result.Error.Message);
        }

        var replaced = invoice.AttachScan(result.Value.Id);
        if (replaced is not null)
        {
            // A blob is immutable, so replacing one means storing the new and enqueueing the old for removal
            var previous = await blobStorage.GetAsync(context, replaced, cancellationToken);
            if (previous.IsSuccess)
            {
                await blobStorage.DeleteOnSaveAsync(context, previous.Value, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
