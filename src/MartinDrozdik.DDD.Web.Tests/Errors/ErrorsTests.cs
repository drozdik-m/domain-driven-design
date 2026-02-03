using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Errors;

public class ErrorsTests(ITestOutputHelper testOutputHelper)
{
    private readonly TestAppFactory _factory = new(testOutputHelper);

    [Fact]
    public async Task Get_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("v1/errors/exception");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problemDetails.Type),
            () => Assert.Equal("An error occurred while processing the request.", problemDetails.Title),
            () => Assert.Equal(500, problemDetails.Status),
            () => Assert.Equal("This is a general exception", problemDetails.Detail),
            () => Assert.Null(problemDetails.Instance),
            () => Assert.True(problemDetails.Extensions.ContainsKey("exception")),
            () => Assert.True(problemDetails.Extensions.ContainsKey("traceId")));
    }

    [Fact]
    public async Task Get_business_rule_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("v1/errors/business-rule-exception");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problemDetails.Type),
            () => Assert.Equal("An error occurred while processing the request.", problemDetails.Title),
            () => Assert.Equal(500, problemDetails.Status),
            () => Assert.Equal("This is a general exception", problemDetails.Detail),
            () => Assert.Null(problemDetails.Instance),
            () => Assert.True(problemDetails.Extensions.ContainsKey("exception")),
            () => Assert.True(problemDetails.Extensions.ContainsKey("traceId")));

        var error1 = problemDetails.Extensions["error1"] as JsonElement?;
        var error2 = problemDetails.Extensions["error2"] as JsonElement?;
        Assert.NotNull(error1);
        Assert.NotNull(error2);

        var error1Value = error1.Value.EnumerateArray().First().GetString();
        var error2Value = error2.Value.EnumerateArray().First().GetString();
        Assert.Multiple(
            () => Assert.Equal("This is error message 1", error1Value),
            () => Assert.Equal("This is error message 2", error2Value));
    }

    [Fact]
    public async Task Get_business_rule_validation_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("v1/errors/business-rule-validation-exception");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationProblemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(validationProblemDetails);
        Assert.NotNull(validationProblemDetails.Errors);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", validationProblemDetails.Type),
            () => Assert.Equal("A validation error occurred while processing the request.", validationProblemDetails.Title),
            () => Assert.Equal(400, validationProblemDetails.Status),
            () => Assert.Equal("This is a general exception", validationProblemDetails.Detail),
            () => Assert.Null(validationProblemDetails.Instance),
            () => Assert.True(validationProblemDetails.Extensions.ContainsKey("traceId")));

        Assert.True(validationProblemDetails.Errors.ContainsKey("Error1"));
        Assert.True(validationProblemDetails.Errors.ContainsKey("Error2"));

        var error1Value = validationProblemDetails.Errors["Error1"].Single();
        var error2Value = validationProblemDetails.Errors["Error2"].Single();
        Assert.Multiple(
            () => Assert.Equal("This is error message 1", error1Value),
            () => Assert.Equal("This is error message 2", error2Value));
    }

    [Fact]
    public async Task Get_validation_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("v1/errors/validation-exception");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationProblemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(validationProblemDetails);
        Assert.NotNull(validationProblemDetails.Errors);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", validationProblemDetails.Type),
            () => Assert.Equal("A validation error occurred while processing the request.", validationProblemDetails.Title),
            () => Assert.Equal(400, validationProblemDetails.Status),
            () => Assert.Equal($"Validation failed: {Environment.NewLine} -- String1: This is error message 1 Severity: Error\r\n -- String2: This is error message 2 Severity: Error", validationProblemDetails.Detail),
            () => Assert.Null(validationProblemDetails.Instance),
            () => Assert.True(validationProblemDetails.Extensions.ContainsKey("traceId")));

        Assert.True(validationProblemDetails.Errors.ContainsKey("String1"));
        Assert.True(validationProblemDetails.Errors.ContainsKey("String2"));

        var error1Value = validationProblemDetails.Errors["String1"].Single();
        var error2Value = validationProblemDetails.Errors["String2"].Single();
        Assert.Multiple(
            () => Assert.Equal("NotEmptyValidator: This is error message 1 ", error1Value),
            () => Assert.Equal("NotEmptyValidator: This is error message 2 ", error2Value));
    }
}
