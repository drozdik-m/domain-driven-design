using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Tests.Errors;

public class ErrorBuilderTests
{
    [Fact]
    public void Build_should_throw_when_code_is_missing()
    {
        var builder = new ErrorBuilder()
            .WithMessage("Message");

        Assert.Throws<ArgumentNullException>(builder.Build);
    }

    [Fact]
    public void Build_should_throw_when_message_is_missing()
    {
        var builder = new ErrorBuilder()
            .WithCode("TEST_CODE");

        Assert.Throws<ArgumentNullException>(builder.Build);
    }

    [Fact]
    public void Can_build_error_with_required_fields_only()
    {
        var error = new ErrorBuilder()
            .WithCode("TEST_CODE")
            .WithMessage("Something went wrong")
            .Build();

        Assert.Equal("TEST_CODE", error.Code.Key);
        Assert.Equal("Something went wrong", error.Message);
        Assert.Empty(error.Details);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Can_build_error_with_single_detail()
    {
        var error = new ErrorBuilder()
            .WithCode("TEST_CODE")
            .WithMessage("Error occurred")
            .WithDetail("field", "is required")
            .Build();

        var detail = Assert.Single(error.Details);
        Assert.Equal("field", detail.Key);
        Assert.Equal("is required", detail.Value);
    }

    [Fact]
    public void Can_build_error_with_multiple_details()
    {
        var details = new[]
        {
            new ErrorDetail("key1", "value1"),
            new ErrorDetail("key2", "value2"),
        };

        var error = new ErrorBuilder()
            .WithCode("TEST_CODE")
            .WithMessage("Error occurred")
            .WithDetails(details)
            .Build();

        Assert.Equal(2, error.Details.Count);
        Assert.True(details.All(d => error.Details.Contains(d)));
    }

    [Fact]
    public void Can_build_error_with_exception()
    {
        var exception = new InvalidOperationException("Boom");

        var error = new ErrorBuilder()
            .WithCode("TEST_CODE")
            .WithMessage("Failure")
            .WithCause(exception)
            .Build();

        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void BuildUnitResult_should_return_failure_with_built_error()
    {
        var result = new ErrorBuilder()
            .WithCode("TEST_CODE")
            .WithMessage("Failure")
            .BuildUnitResult();

        Assert.True(result.IsFailure);
    }
}
