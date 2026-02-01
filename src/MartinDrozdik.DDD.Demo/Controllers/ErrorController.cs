using System.Net.Mime;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MartinDrozdik.DDD.Demo.Controllers;

[ApiController]
[Route("v1/errors")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
[ProducesResponseType<ValidationProblem>(StatusCodes.Status400BadRequest)]
public class ErrorController : ControllerBase
{
    [HttpGet("exception")]
    [Produces("application/json")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetException(CancellationToken cancellationToken)
    {
        throw new Exception("This is a general exception");
    }

    [HttpGet("business-rule-exception")]
    [Produces("application/json")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> GetBusinessRuleException(CancellationToken cancellationToken)
    {
        throw new BusinessRuleException("This is a general exception")
        {
            Details =
            [
                new ExceptionDetail("Error1", "This is error message 1"),
                new ExceptionDetail("Error2", "This is error message 2")
            ],
        };
    }

    [HttpGet("business-rule-validation-exception")]
    [Produces("application/json")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> GetBusinessRuleValidationException()
    {
        throw new BusinessRuleValidationException("This is a general exception")
        {
            Details =
            [
                new ExceptionDetail("Error1", "This is error message 1"),
                new ExceptionDetail("Error2", "This is error message 2")
            ],
        };
    }

    [HttpGet("validation-exception")]
    [Produces("application/json")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> GetValidationException()
    {
        var validator = new ErrorValidator();
        var errorClass = new ErrorClass();
        validator.ValidateAndThrow(errorClass);
        return Ok();
    }

    public class ErrorClass
    {
        public string String1 { get; set; } = string.Empty;

        public string String2 { get; set; } = string.Empty;
    }

    private class ErrorValidator : AbstractValidator<ErrorClass>
    {
        public ErrorValidator()
        {
            RuleFor(e => e.String1).NotEmpty().WithMessage("This is error message 1");
            RuleFor(e => e.String2).NotEmpty().WithMessage("This is error message 2");
        }
    }
}
