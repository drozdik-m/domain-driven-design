using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Options.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Add_app_options_binds_the_configured_section()
    {
        // Arrange
        const string value = "Hello there";
        var provider = BuildProvider(
            services => services.AddAppOptions<TestOptions>(),
            ("Test:Abc:SomeString", value));

        // Act
        var options = provider.GetRequiredService<IOptions<TestOptions>>();

        // Assert
        Assert.Equal(value, options.Value.SomeString);
    }

    [Fact]
    public void Add_app_options_binds_nested_sections()
    {
        // Arrange
        const string value = "Hello there";
        var provider = BuildProvider(
            services => services.AddAppOptions<ComposedTestOptions>(),
            ("Test:Def:Foo:SomeInnerString", value));

        // Act
        var options = provider.GetRequiredService<IOptions<ComposedTestOptions>>();

        // Assert
        Assert.Equal(value, options.Value.Foo.SomeInnerString);
    }

    [Fact]
    public void Add_app_options_rejects_unknown_configuration_keys()
    {
        // Arrange
        var provider = BuildProvider(
            services => services.AddAppOptions<TestOptions>(),
            ("Test:Abc:SomeString", "Hello there"),
            ("Test:Abc:TypoedKey", "Oops"));

        // Act
        var exception = Record.Exception(() => provider.GetRequiredService<IOptions<TestOptions>>().Value);

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Add_validated_app_options_registers_the_fluent_validator()
    {
        // Arrange
        var provider = BuildProvider(
            services => services.AddValidatedAppOptions<TestOptions>(),
            ("Test:Abc:SomeString", "Hello there"));

        // Act
        var validators = provider.GetServices<IValidateOptions<TestOptions>>();

        // Assert
        Assert.Contains(validators, e => e is FluentValidateOptions<TestOptions>);
    }

    [Fact]
    public void Add_validated_app_options_accepts_valid_configuration()
    {
        // Arrange
        const string value = "Hello there";
        var provider = BuildProvider(
            services => services.AddValidatedAppOptions<TestOptions>(),
            ("Test:Abc:SomeString", value));

        // Act
        var exception = Record.Exception(provider.GetRequiredService<IStartupValidator>().Validate);

        // Assert
        Assert.Null(exception);
        Assert.Equal(value, provider.GetRequiredService<IOptions<TestOptions>>().Value.SomeString);
    }

    [Fact]
    public void Add_validated_app_options_rejects_invalid_configuration()
    {
        // Arrange
        var provider = BuildProvider(services => services.AddValidatedAppOptions<TestOptions>());

        // Act
        var validator = provider.GetRequiredService<IStartupValidator>();

        // Assert
        Assert.Throws<OptionsValidationException>(validator.Validate);
    }

    [Fact]
    public void Add_validated_app_options_rejects_invalid_nested_configuration()
    {
        // Arrange
        var provider = BuildProvider(services => services.AddValidatedAppOptions<ComposedTestOptions>());

        // Act
        var validator = provider.GetRequiredService<IStartupValidator>();

        // Assert
        Assert.Throws<OptionsValidationException>(validator.Validate);
    }

    /// <summary>
    /// Builds a service provider with an in-memory configuration.
    /// </summary>
    /// <param name="configure">Registers the options under test.</param>
    /// <param name="settings">Configuration keys and values to expose.</param>
    /// <returns>The built <see cref="ServiceProvider"/>.</returns>
    private static ServiceProvider BuildProvider(
        Action<IServiceCollection> configure,
        params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        configure(services);

        return services.BuildServiceProvider();
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

    private class ComposedTestOptions : IValidatedAppOptions<ComposedTestOptions>
    {
        public static string Section { get; } = "Test:Def";

        public static AbstractValidator<ComposedTestOptions> Validator { get; } = new OptionsValidation();

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public required FooClass Foo { get; init; }
#pragma warning restore S3459 // Unassigned members should be removed
#pragma warning restore S1144 // Unused private types or members should be removed

        private class OptionsValidation : AbstractValidator<ComposedTestOptions>
        {
            public OptionsValidation()
            {
                RuleFor(e => e.Foo).NotNull();
                RuleFor(e => e.Foo.SomeInnerString).NotEmpty().When(e => e.Foo is not null);
            }
        }
    }

    private class FooClass
    {
        public string SomeInnerString { get; set; } = string.Empty;
    }
}
