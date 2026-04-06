using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Errors;

/// <summary>
/// Base class for error handling tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
public abstract class ErrorHandlingTests<TProgram> : IDisposable
    where TProgram : class
{
    private readonly TestedApp<TProgram> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingTests{TProgram}"/> class.
    /// </summary>
    /// <param name="factory">App factory under test. Disposed automatically.</param>
    protected ErrorHandlingTests(TestedAppBuilder<TProgram> factory)
    {
        _factory = factory
            .WithEndpoints(e => e.MapErrorEndpoints())
            .Build();
    }

    /// <summary>
    /// Asserts basic <see cref="Exception"/> to test the general error handling pipeline.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Get_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(ErrorEndpoints.BasePath + "/exception", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
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
    /*
    /// <summary>
    /// Asserts <see cref="BusinessNotFoundException"/> to test the general error handling pipeline.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Get_business_not_found_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(ErrorEndpoints.BasePath + "/not-found-exception", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
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
    }*/

    /// <summary>
    /// Asserts <see cref="BusinessRuleException"/> to test the general error handling pipeline.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Get_business_rule_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(ErrorEndpoints.BasePath + "/business-rule-exception", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
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

    /// <summary>
    /// Asserts <see cref="BusinessRuleValidationException"/> to test the general error handling pipeline.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Get_business_rule_validation_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(ErrorEndpoints.BasePath + "/business-rule-validation-exception", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationProblemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
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

    /// <summary>
    /// Asserts <see cref="ValidationException"/> to test the general error handling pipeline.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [Fact]
    public async Task Get_validation_exception()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(ErrorEndpoints.BasePath + "/validation-exception", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var validationProblemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(validationProblemDetails);
        Assert.NotNull(validationProblemDetails.Errors);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", validationProblemDetails.Type),
            () => Assert.Equal("A validation error occurred while processing the request.", validationProblemDetails.Title),
            () => Assert.Equal(400, validationProblemDetails.Status),
            () => Assert.Equal($"Validation failed: {Environment.NewLine} -- String1: This is error message 1 Severity: Error{Environment.NewLine} -- String2: This is error message 2 Severity: Error", validationProblemDetails.Detail),
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }
    }
}
