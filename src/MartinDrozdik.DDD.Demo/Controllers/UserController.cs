using System.Net.Mime;
using System.Security.Claims;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/user")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
[ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
public class UserController : ControllerBase
{
    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    public ActionResult<UserInfo> GetUserInfo()
    {
        var user = User ?? throw new BusinessNotFoundException();

        return new UserInfo
        {
            Id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Name = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
            Roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value),
        };
    }
}

public class UserInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IEnumerable<string> Roles { get; init; }
}
