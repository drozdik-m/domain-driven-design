using FluentValidation;
using FluentValidation.Results;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Tests.Extensions;

public class ValidationExtensionsTests
{
    [Fact]
    public void TryGetError_should_return_false_for_valid_result()
    {
        var result = new ValidationResult();

        var hasError = result.TryGetError(out var error);

        Assert.False(hasError);
        Assert.Null(error);
    }

    [Fact]
    public void TryGetError_should_return_true_and_error_for_invalid_result()
    {
        var result = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required")
            {
                ErrorCode = "REQUIRED",
                AttemptedValue = null,
            },
        });

        var hasError = result.TryGetError(out var error);

        Assert.True(hasError);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidObject, error.Code);
        Assert.Single(error.Details);
    }

    [Fact]
    public void GetError_should_create_error_with_single_invariant_message()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "is required")
            {
                ErrorCode = "REQUIRED",
                AttemptedValue = null,
            },
        };

        var error = failures.GetError();

        Assert.Equal(ErrorCodes.InvalidObject, error.Code);
        Assert.Equal(WellKnownErrorsResource.InvariantError, error.Message);
        Assert.Single(error.Details);
    }

    [Fact]
    public void GetError_should_create_error_with_plural_invariant_message()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "is required") { ErrorCode = "REQUIRED" },
            new ValidationFailure("Age", "must be greater than 0") { ErrorCode = "INVALID" },
        };

        var error = failures.GetError();

        Assert.Equal(ErrorCodes.InvalidObject, error.Code);
        Assert.Equal(WellKnownErrorsResource.InvariantErrors, error.Message);
        Assert.Equal(2, error.Details.Count);
        Assert.Multiple(
            () => Assert.Contains(error.Details, e => e.Key == "Name"),
            () => Assert.Contains(error.Details, e => e.Key == "Age"));
    }

    [Fact]
    public void GetException_should_return_business_rule_validation_exception()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "is required") { ErrorCode = "REQUIRED" },
        };

        var exception = failures.GetException();

        Assert.IsType<BusinessRuleValidationException>(exception);
        Assert.Equal(WellKnownErrorsResource.InvariantError, exception.Message);
        Assert.Single(exception.Details);
        Assert.Contains(exception.Details, e => e.Key == "Name");
    }

    [Fact]
    public void ToBusinessRuleException_should_convert_error_correctly()
    {
        var error = new ErrorBuilder()
            .WithCode(ErrorCodes.InvalidObject)
            .WithMessage("Failure")
            .WithDetail("field", "invalid")
            .Build();

        var exception = error.ToBusinessRuleException();

        Assert.IsType<BusinessRuleException>(exception);
        Assert.Equal("Failure", exception.Message);
        Assert.Single(exception.Details);
        Assert.Contains(exception.Details, e => e.Key == "field" && e.Value == "invalid");
    }

    [Fact]
    public void ToBusinessRuleException_should_convert_not_found_error_correctly()
    {
        var error = new ErrorBuilder()
            .WithCode(ErrorCodes.NotFound)
            .WithMessage("Failure")
            .WithDetail("field", "invalid")
            .Build();

        var exception = error.ToBusinessRuleException();

        Assert.IsType<BusinessNotFoundException>(exception);
        Assert.Equal("Failure", exception.Message);
        Assert.Single(exception.Details);
        Assert.Contains(exception.Details, e => e.Key == "field" && e.Value == "invalid");
    }

    [Fact]
    public void TryGetException_should_return_false_for_valid_result()
    {
        var result = new ValidationResult();

        var hasException = result.TryGetException(out var exception);

        Assert.False(hasException);
        Assert.Null(exception);
    }

    [Fact]
    public void TryGetException_should_return_true_and_exception_for_invalid_result()
    {
        var result = new ValidationResult(new[]
        {
           new ValidationFailure("Name", "is required") { ErrorCode = "REQUIRED" },
        });

        var hasException = result.TryGetException(out var exception);

        Assert.True(hasException);
        Assert.IsType<BusinessRuleValidationException>(exception);
        Assert.Equal(WellKnownErrorsResource.InvariantError, exception.Message);
        Assert.Single(exception.Details);
        Assert.Contains(exception.Details, e => e.Key == "Name");
    }

    [Fact]
    public void BuildBusinessException_should_build_and_convert_error()
    {
        var exception = new ErrorBuilder()
            .WithCode(ErrorCodes.InvalidObject)
            .WithMessage("Failure")
            .BuildBusinessException();

        Assert.IsType<BusinessRuleException>(exception);
        Assert.Equal("Failure", exception.Message);
    }

    [Fact]
    public void BuildValidationException_should_build_and_convert_error()
    {
        var exception = new ErrorBuilder()
            .WithCode(ErrorCodes.InvalidObject)
            .WithMessage("Failure")
            .BuildValidationException();

        Assert.IsType<BusinessRuleValidationException>(exception);
        Assert.Equal("Failure", exception.Message);
    }

    [Fact]
    public void ValidateAndThrowBusiness_should_throw_for_invalid_instance()
    {
        var validator = new TestValidator();
        Assert.Throws<BusinessRuleValidationException>(() =>
            validator.ValidateAndThrowBusiness(new TestModel()));
    }

    [Fact]
    public async Task ValidateAndThrowBusinessAsync_should_throw_for_invalid_instance()
    {
        var validator = new TestValidator();
        await Assert.ThrowsAsync<BusinessRuleValidationException>(async () =>
            await validator.ValidateAndThrowBusinessAsync(new TestModel(), CancellationToken.None));
    }

    private sealed class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestValidator : AbstractValidator<TestModel>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode("REQUIRED");
        }
    }
}
