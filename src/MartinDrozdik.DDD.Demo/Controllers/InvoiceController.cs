using System.Net.Mime;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.RecurringTasks;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/invoice")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class InvoiceController(
    IMediator mediator,
    IRecurringTaskTrigger<InvoiceVolumeReportTask> volumeReportTrigger) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<GetInvoicesQuery.Response>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetInvoicesQuery.Response>> Get(CancellationToken cancellationToken)
    {
        var query = new GetInvoicesQuery();
        var result = await mediator.SendQuery<GetInvoicesQuery, GetInvoicesQuery.Response>(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType<InvoiceId>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceId>> SaveDraft([FromBody]CreateInvoiceDraftCommand.Request request, CancellationToken cancellationToken)
    {
        var command = new CreateInvoiceDraftCommand(request);
        var result = await mediator.SendCommand<CreateInvoiceDraftCommand, InvoiceId>(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Attaches a scanned document to an invoice, replacing any already attached.
    /// </summary>
    [HttpPost("{id:guid}/scan")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> AttachScan(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var command = new AttachInvoiceScanCommand(id, content, file.FileName, file.ContentType);
        var scanId = await mediator.SendCommand<AttachInvoiceScanCommand, BlobId>(command, cancellationToken);

        return Ok(scanId.Key);
    }

    /// <summary>
    /// Downloads the scanned document of an invoice.
    /// </summary>
    [HttpGet("{id:guid}/scan")]
    [Produces(MediaTypeNames.Application.Octet)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScan(Guid id, CancellationToken cancellationToken)
    {
        var scan = await mediator.SendQuery<GetInvoiceScanQuery, BlobContent>(new GetInvoiceScanQuery(id), cancellationToken);

        // Blobs are immutable, so the checksum is a strong entity tag
        if (scan.Blob.Checksum is { } checksum)
        {
            Response.Headers.ETag = checksum.ToETag();
        }

        return File(scan.Content, scan.Blob.ContentType.Value, scan.Blob.OriginalFileName);
    }

    /// <summary>
    /// Removes the scanned document of an invoice.
    /// </summary>
    [HttpDelete("{id:guid}/scan")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveScan(Guid id, CancellationToken cancellationToken)
    {
        await mediator.SendCommand(new RemoveInvoiceScanCommand(id), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Runs the invoice volume report now instead of waiting for its next scheduled run.
    /// </summary>
    [HttpPost("volume-report")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult RunVolumeReport()
    {
        // Returns straight away - the background loop picks the request up and does the work
        volumeReportTrigger.Trigger();
        return Accepted();
    }
}
