using System.Net.Mime;
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
