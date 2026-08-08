using FluentValidation;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.Extensions.Configuration;

namespace MartinDrozdik.DDD.Options.Tests;

public class ConfigurationManagerExtensionsTests
{
    [Fact]
    public void Get_options_returns_the_bound_section()
    {
        // Arrange
        const string value = "Hello there";
        var configuration = BuildConfiguration(("Test:Abc:SomeString", value));

        // Act
        var options = configuration.GetOptions<TestOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(value, options.SomeString);
    }

    [Fact]
    public void Get_options_returns_null_for_a_missing_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Unrelated:Key", "Whatever"));

        // Act
        var options = configuration.GetOptions<TestOptions>();

        // Assert
        Assert.Null(options);
    }

    [Fact]
    public void Get_required_options_returns_the_bound_section()
    {
        // Arrange
        const string value = "Hello there";
        var configuration = BuildConfiguration(("Test:Abc:SomeString", value));

        // Act
        var options = configuration.GetRequiredOptions<TestOptions>();

        // Assert
        Assert.Equal(value, options.SomeString);
    }

    [Fact]
    public void Get_required_options_throws_for_a_missing_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Unrelated:Key", "Whatever"));

        // Act
        var exception = Record.Exception(configuration.GetRequiredOptions<TestOptions>);

        // Assert
        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains(TestOptions.Section, invalidOperation.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_validated_options_returns_the_bound_section_when_valid()
    {
        // Arrange
        const string value = "Hello there";
        var configuration = BuildConfiguration(("Test:Abc:SomeString", value));

        // Act
        var options = configuration.GetValidatedOptions<TestOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(value, options.SomeString);
    }

    [Fact]
    public void Get_validated_options_returns_null_for_a_missing_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Unrelated:Key", "Whatever"));

        // Act
        var options = configuration.GetValidatedOptions<TestOptions>();

        // Assert
        Assert.Null(options);
    }

    [Fact]
    public void Get_validated_options_throws_for_an_invalid_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Test:Abc:SomeString", string.Empty));

        // Act
        var exception = Record.Exception(configuration.GetValidatedOptions<TestOptions>);

        // Assert
        Assert.IsType<BusinessRuleValidationException>(exception);
    }

    [Fact]
    public void Get_required_validated_options_throws_for_a_missing_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Unrelated:Key", "Whatever"));

        // Act
        var exception = Record.Exception(configuration.GetRequiredValidatedOptions<TestOptions>);

        // Assert
        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains(TestOptions.Section, invalidOperation.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_required_validated_options_throws_for_an_invalid_section()
    {
        // Arrange
        var configuration = BuildConfiguration(("Test:Abc:SomeString", string.Empty));

        // Act
        var exception = Record.Exception(configuration.GetRequiredValidatedOptions<TestOptions>);

        // Assert
        Assert.IsType<BusinessRuleValidationException>(exception);
    }

    /// <summary>
    /// Builds an <see cref="IConfigurationManager"/> backed by an in-memory source - no host, no web.
    /// </summary>
    /// <param name="settings">Configuration keys and values to expose.</param>
    /// <returns>The populated <see cref="ConfigurationManager"/>.</returns>
    private static ConfigurationManager BuildConfiguration(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(settings.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)));

        return configuration;
    }

    private class TestOptions : IValidatedAppOptions<TestOptions>
    {
        public static string Section { get; } = "Test:Abc";

        public static AbstractValidator<TestOptions> Validator { get; } = new OptionsValidation();

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public required string SomeString { get; init; }
#pragma warning restore S3459 // Unassigned members should be removed
#pragma warning restore S1144 // Unused private types or members should be removed

        private class OptionsValidation : AbstractValidator<TestOptions>
        {
            public OptionsValidation()
            {
                RuleFor(e => e.SomeString).NotEmpty();
            }
        }
    }
}
