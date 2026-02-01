using MartinDrozdik.DDD.Demo.Client.Generated.Models;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.Enumerations;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Serialization;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Errors;

public class ErrorsTests(ITestOutputHelper testOutputHelper)
{
    private readonly DemoAppFactory _factory = new(testOutputHelper);

    [Fact]
    public async Task Get_exception()
    {
        // Arrange
        var client = _factory.CreateDddClient();

        // Act
        var exception = await Assert.ThrowsAsync<ProblemDetails>(() => client.V1.Errors.Exception.GetAsync(cancellationToken: CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", exception.Type),
            () => Assert.Equal("An error occurred while processing the request.", exception.Title),
            () => Assert.Equal(500, exception.ResponseStatusCode),
            () => Assert.Equal("This is a general exception", exception.Detail),
            () => Assert.Null(exception.Instance),
            () => Assert.NotNull(exception.AdditionalData["exception"]),
            () => Assert.NotNull(exception.AdditionalData["traceId"]));
    }

    [Fact]
    public async Task Get_business_rule_exception()
    {
        // Arrange
        var client = _factory.CreateDddClient();

        // Act
        var exception = await Assert.ThrowsAsync<ProblemDetails>(() => client.V1.Errors.BusinessRuleException.GetAsync(cancellationToken: CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", exception.Type),
            () => Assert.Equal("An error occurred while processing the request.", exception.Title),
            () => Assert.Equal(500, exception.ResponseStatusCode),
            () => Assert.Equal("This is a general exception", exception.Detail),
            () => Assert.Null(exception.Instance),
            () => Assert.NotNull(exception.AdditionalData["exception"]),
            () => Assert.NotNull(exception.AdditionalData["traceId"]));

        var error1 = exception.AdditionalData["error1"] as UntypedArray;
        var error2 = exception.AdditionalData["error2"] as UntypedArray;
        Assert.NotNull(error1);
        Assert.NotNull(error2);
        var error1Value = error1.GetValue().Single() as UntypedString;
        var error2Value = error2.GetValue().Single() as UntypedString;
        Assert.NotNull(error1Value);
        Assert.NotNull(error2Value);
        Assert.Multiple(
            () => Assert.Equal("This is error message 1", error1Value.GetValue()),
            () => Assert.Equal("This is error message 2", error2Value.GetValue()));
    }

    [Fact]
    public async Task Get_business_rule_validation_exception()
    {
        // Arrange
        var client = _factory.CreateDddClient();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationProblem>(() => client.V1.Errors.BusinessRuleValidationException.GetAsync(cancellationToken: CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        /*Assert.Multiple(
            () => Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", exception.Type),
            () => Assert.Equal("A validation error occurred while processing the request.", exception.Title),
            () => Assert.Equal(400, exception.ResponseStatusCode),
            () => Assert.Equal("This is a general exception", exception.Detail),
            () => Assert.Null(exception.Instance),
            () => Assert.NotNull(exception.AdditionalData["exception"]),
            () => Assert.NotNull(exception.AdditionalData["traceId"]),
            () => Assert.Equal("This is error message 1", exception.),
            () => Assert.Equal("This is error message 2", exception.AdditionalData["error2"]));*/
    }
}
