using MartinDrozdik.DDD.Web.FilePathProviders.Static;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MartinDrozdik.DDD.Web.Tests.FilePathProviders.Static;

public class TimestampedStaticFilePathProviderTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("/a/b/c")]
    [InlineData("/a/b/c.css")]
    [InlineData("yep.css")]
    [InlineData("/a/b/c.js")]
    [InlineData("a/b/c.js")]
    [InlineData("interesting.png")]
    public async Task Timestamp_is_added(string path)
    {
        // Arrange
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.RemoveAll<IStaticFilePathProvider>())
            .WithServices(services => services.AddSingleton<IStaticFilePathProvider, TimestampedStaticFilePathProvider>())
            .WithFakeTime(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .Build();
        var service = factory.Services.GetRequiredService<IStaticFilePathProvider>();

        // Act
        var result = service.PathTo(path);

        // Assert
        Assert.Equal($"{path}?version=946684800000", result);
    }
}
