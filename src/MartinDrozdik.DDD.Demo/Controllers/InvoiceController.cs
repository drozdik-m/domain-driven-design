using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("[controller]")]
public class InvoiceController : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult Get()
    {
        return Ok();
    }
}
