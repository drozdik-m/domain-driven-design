using MartinDrozdik.DDD.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.Options;

public class WebHostBuilderExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Set_test_options_are_set_correctly()
    {
        // Arrange
        const string value = "Hello there";
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<TestOptions>(e => e.SomeString, value)
            .WithServices(services => services.AddAppOptions<TestOptions>())
            .Build();

        // Act
        var options = factory.Services.GetRequiredService<IOptions<TestOptions>>();

        // Assert
        Assert.Equal(value, options.Value.SomeString);
    }

    [Fact]
    public async Task Set_deeper_test_options_are_set_correctly()
    {
        // Arrange
        const string value = "Hello there";
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<DeeperTestOptions>(e => e.SomeClass.DeepString, value)
            .WithServices(services => services.AddAppOptions<DeeperTestOptions>())
            .Build();

        // Act
        var options = factory.Services.GetRequiredService<IOptions<DeeperTestOptions>>();

        // Assert
        Assert.Equal(value, options.Value.SomeClass.DeepString);
    }

    private class TestOptions : IAppOptions
    {
        public static string Section { get; } = "Test:Abcd";

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public required string SomeString { get; init; }
#pragma warning restore S3459 // Unassigned members should be removed
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private class DeeperTestOptions : IAppOptions
    {
        public static string Section { get; } = "Test:Abcd";

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public required InnerClass SomeClass { get; init; }
#pragma warning restore S3459 // Unassigned members should be removed
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private class InnerClass
    {
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public required string DeepString { get; init; }
#pragma warning restore S3459 // Unassigned members should be removed
#pragma warning restore S1144 // Unused private types or members should be removed
    }
}
