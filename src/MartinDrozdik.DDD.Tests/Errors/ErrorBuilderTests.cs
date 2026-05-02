using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Tests.Errors;

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
    public void Can_build_error_with_sub_errors()
    {
        var subError1 = new ErrorBuilder()
            .WithCode("FIELD_REQUIRED")
            .WithMessage("Field is required")
            .WithDetail("field", "name")
            .Build();

        var subError2 = new ErrorBuilder()
            .WithCode("INVALID_VALUE")
            .WithMessage("Invalid value")
            .Build();

        var error = new ErrorBuilder()
            .WithCode("VALIDATION_FAILED")
            .WithMessage("Validation failed")
            .WithSubErrors(subError1, subError2)
            .Build();

        Assert.Equal(3, error.Details.Count);

        Assert.Contains(error.Details, d =>
            d.Key == "FIELD_REQUIRED" &&
            d.Value == "Field is required");

        Assert.Contains(error.Details, d =>
            d.Key == "INVALID_VALUE" &&
            d.Value == "Invalid value");

        Assert.Contains(error.Details, d =>
            d.Key == "field" &&
            d.Value == "name");
    }

    [Fact]
    public void WithSubErrors_should_ignore_sub_error_exceptions()
    {
        var subError = new ErrorBuilder()
            .WithCode("INNER_ERROR")
            .WithMessage("Inner failure")
            .WithCause(new InvalidOperationException("Boom"))
            .Build();

        var error = new ErrorBuilder()
            .WithCode("OUTER_ERROR")
            .WithMessage("Outer failure")
            .WithSubErrors(subError)
            .Build();

        Assert.Null(error.Exception);

        var detail = Assert.Single(error.Details);
        Assert.Equal("INNER_ERROR", detail.Key);
        Assert.Equal("Inner failure", detail.Value);
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
