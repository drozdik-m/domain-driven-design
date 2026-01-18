using FluentValidation;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Tests.Options;

public class FluentValidateOptionsTests
{
    [Fact]
    public void Validate_with_valid_options_returns_success()
    {
        // Arrange
        var validator = new FluentValidateOptions<ValidTestOptions>();
        var options = new ValidTestOptions { SomeString = "Valid value" };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_with_invalid_options_returns_failure()
    {
        // Arrange
        var validator = new FluentValidateOptions<ValidTestOptions>();
        var options = new ValidTestOptions { SomeString = string.Empty };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures, e => e.Contains(nameof(ValidTestOptions.SomeString)));
    }

    [Fact]
    public void Validate_with_multiple_validation_failures_returns_all_failures()
    {
        // Arrange
        var validator = new FluentValidateOptions<MultiRuleTestOptions>();
        var options = new MultiRuleTestOptions
        {
            SomeString = string.Empty,
            SomeNumber = -1,
        };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.NotNull(result.Failures);

        var failures = result.Failures.ToList();
        Assert.True(failures.Count >= 2);
        Assert.Contains(failures, e => e.Contains(nameof(MultiRuleTestOptions.SomeString)));
        Assert.Contains(failures, e => e.Contains(nameof(MultiRuleTestOptions.SomeNumber)));
    }

    [Fact]
    public void Validate_includes_option_type_name_in_failure_message()
    {
        // Arrange
        var validator = new FluentValidateOptions<ValidTestOptions>();
        var options = new ValidTestOptions { SomeString = string.Empty };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures, e => e.Contains(nameof(ValidTestOptions)));
    }

    private class ValidTestOptions : IValidatedAppOptions<ValidTestOptions>
    {
        public static string Section { get; } = "Test:Valid";

        public static AbstractValidator<ValidTestOptions> Validator { get; } = new OptionsValidation();

        public required string SomeString { get; init; }

        private class OptionsValidation : AbstractValidator<ValidTestOptions>
        {
            public OptionsValidation()
            {
                RuleFor(e => e.SomeString).NotEmpty();
            }
        }
    }

    private class MultiRuleTestOptions : IValidatedAppOptions<MultiRuleTestOptions>
    {
        public static string Section { get; } = "Test:MultiRule";

        public static AbstractValidator<MultiRuleTestOptions> Validator { get; } = new OptionsValidation();

        public required string SomeString { get; init; }

        public required int SomeNumber { get; init; }

        private class OptionsValidation : AbstractValidator<MultiRuleTestOptions>
        {
            public OptionsValidation()
            {
                RuleFor(e => e.SomeString).NotEmpty().WithMessage("must not be empty");
                RuleFor(e => e.SomeNumber).GreaterThanOrEqualTo(0).WithMessage("must be non-negative");
            }
        }
    }
}
