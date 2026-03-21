using FluentValidation;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.Options;

public class OptionsExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task App_builds_successfully_by_default()
    {
        // Arrange
        var factory = new TestProgramFactoryBuilder(testOutputHelper).Build();

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
        var factory = new TestProgramFactoryBuilder(testOutputHelper)
            .WithOption<TestOptions>(e => e.SomeString, value)
            .WithServices(services => services.AddAppOptions<TestOptions>())
            .Build();

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
        var factory = new TestProgramFactoryBuilder(testOutputHelper)
            .WithOption<TestOptions>(e => e.SomeString, value)
            .WithServices(services => services.AddValidatedAppOptions<TestOptions>())
            .Build();

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
        var factory = new TestProgramFactoryBuilder(testOutputHelper)
            .WithServices(services => services.AddValidatedAppOptions<TestOptions>())
            .Build();

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
