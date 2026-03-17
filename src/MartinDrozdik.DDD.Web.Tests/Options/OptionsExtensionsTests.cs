using FluentValidation;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Options;

public class OptionsExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task App_builds_successfully_by_default()
    {
        // Arrange
        var factory = new TestWebApplicationFactoryBuilder<TestProgram>(testOutputHelper).Build();

        // Act
        var exception = Record.Exception(factory.StartServer);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task App_with_test_options_loads_correctly()
    {
        // Arrange
        const string value = "Hello there";
        var factory = new TestAppFactory(testOutputHelper, config =>
        {
            config.UseSetting($"{TestOptions.Section}:{nameof(TestOptions.SomeString)}", value);
            config.ConfigureServices(services =>
            {
                services.AddAppOptions<TestOptions>();
            });
        });

        // Act
        var exception = Record.Exception(factory.StartServer);

        // Assert
        Assert.Null(exception);
        var options = factory.Services.GetRequiredService<IOptions<TestOptions>>();
        Assert.Equal(value, options.Value.SomeString);
    }

    [Fact]
    public async Task App_with_validated_test_options_loads_correctly()
    {
        // Arrange
        const string value = "Hello there";
        var factory = new TestAppFactory(testOutputHelper, config =>
        {
            config.UseSetting($"{TestOptions.Section}:{nameof(TestOptions.SomeString)}", value);
            config.ConfigureServices(services =>
            {
                services.AddValidatedAppOptions<TestOptions>();
            });
        });

        // Act
        var exception = Record.Exception(factory.StartServer);

        // Assert
        Assert.Null(exception);
        var options = factory.Services.GetRequiredService<IOptions<TestOptions>>();
        Assert.Equal(value, options.Value.SomeString);
    }

    [Fact]
    public async Task App_with_invalid_options_fails_to_run()
    {
        // Arrange
        var factory = new TestAppFactory(testOutputHelper, config =>
        {
            config.ConfigureServices(services =>
            {
                services.AddValidatedAppOptions<TestOptions>();
            });
        });

        Assert.Throws<OptionsValidationException>(factory.StartServer);
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
