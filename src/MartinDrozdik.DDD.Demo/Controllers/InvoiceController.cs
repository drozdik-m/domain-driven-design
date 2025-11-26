using MartinDrozdik.DDD.Demo.Requests.Invoice;
using MartinDrozdik.DDD.Models.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/invoice")]
public class InvoiceController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetInvoicesQuery.Response>> Get(CancellationToken cancellationToken)
    {
        var query = new GetInvoicesQuery();
        var result = await mediator.SendQuery<GetInvoicesQuery, GetInvoicesQuery.Response>(query, cancellationToken);

        if (result.IsFailure)
        {
            // TODO: Handle errors properly
            return StatusCode(500, result.Error);
        }

        return Ok(result.Value);
    }
}
