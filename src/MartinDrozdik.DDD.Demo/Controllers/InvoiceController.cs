using System.Net.Mime;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Models.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/invoice")]
[Produces(MediaTypeNames.Application.Json)]
public class InvoiceController(
    IMediator mediator) : ControllerBase
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
        return Ok(result);
    }
}
