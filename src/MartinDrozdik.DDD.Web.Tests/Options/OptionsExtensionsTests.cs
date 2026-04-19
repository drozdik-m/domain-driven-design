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
        var factory = new TestedWebAppBuilder(testOutputHelper).Build();

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
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<TestOptions>(e => e.SomeString, value)
            .WithOption<ComposedTestOptions>(e => e.Foo.SomeInnerString, value)
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
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<TestOptions>(e => e.SomeString, value)
            .WithOption<ComposedTestOptions>(e => e.Foo.SomeInnerString, value)
            .WithServices(services => services.AddValidatedAppOptions<TestOptions>())
            .WithServices(services => services.AddValidatedAppOptions<ComposedTestOptions>())
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
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.AddValidatedAppOptions<TestOptions>())
            .Build();

        Assert.Throws<OptionsValidationException>(factory.StartServer);
    }

    [Fact]
    public async Task App_with_invalid_nested_options_fails_to_run()
    {
        // Arrange
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.AddValidatedAppOptions<ComposedTestOptions>())
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
