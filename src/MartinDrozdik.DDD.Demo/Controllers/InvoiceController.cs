using System.Net.Mime;
using MartinDrozdik.DDD.Demo.Requests.Invoice;
using MartinDrozdik.DDD.Models.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/invoice")]
[Produces(MediaTypeNames.Application.Json)]
public class InvoiceController(
    IMediator mediator,
    ILogger<InvoiceController> logger) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<GetInvoicesQuery.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetInvoicesQuery.Response>> Get(CancellationToken cancellationToken)
    {
        var query = new GetInvoicesQuery();
        var result = await mediator.SendQuery<GetInvoicesQuery, GetInvoicesQuery.Response>(query, cancellationToken);

        if (result.IsFailure)
        {
            // TODO: Handle errors properly, add error responses and middlewares and error handlers
            logger.LogError("Error occurred while getting invoices: {Error}", result.Error);
            return StatusCode(500, result.Error);
        }

        return Ok(result.Value);
    }
}
