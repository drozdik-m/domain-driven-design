using MartinDrozdik.DDD.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Options;

public class WebHostBuilderExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Set_test_options_are_set_correctly()
    {
        // Arrange
        const string value = "Hello there";
        var factory = new TestAppFactory(testOutputHelper, config =>
        {
            config.SetOption<TestOptions>(e => e.SomeString, value);
            config.ConfigureServices(services =>
            {
                services.AddAppOptions<TestOptions>();
            });
        });

        // Act
        var options = factory.Services.GetRequiredService<IOptions<TestOptions>>();

        // Assert
        Assert.Equal(value, options.Value.SomeString);
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
}
