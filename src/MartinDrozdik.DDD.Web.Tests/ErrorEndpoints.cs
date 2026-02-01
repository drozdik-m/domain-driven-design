using System.Net.Mime;
using FluentValidation;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MartinDrozdik.DDD.Web.Tests;

public static class ErrorEndpoints
{
    public static RouteGroupBuilder MapErrorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("v1/errors").WithTags("Errors");

        group.MapGet("exception", GetException)
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("business-rule-exception", GetBusinessRuleException)
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("business-rule-validation-exception", GetBusinessRuleValidationException)
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("validation-exception", GetValidationException)
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<string> GetException()
    {
        throw new Exception("This is a general exception");
    }

    private static string GetBusinessRuleException()
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

    private static string GetBusinessRuleValidationException()
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

    private static IResult GetValidationException()
    {
        var validator = new ErrorValidator();
        var errorClass = new ErrorClass();
        validator.ValidateAndThrow(errorClass);
        return Results.Ok();
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
