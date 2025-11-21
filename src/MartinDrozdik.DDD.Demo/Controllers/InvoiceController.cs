using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/invoice")]
public class InvoiceController : ControllerBase
{
    [HttpGet(Name = "GetInvoice")]
    public ActionResult Get()
    {
        return Ok();
    }
}
