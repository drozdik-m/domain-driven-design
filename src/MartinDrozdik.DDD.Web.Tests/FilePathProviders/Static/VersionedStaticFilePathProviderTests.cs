using MartinDrozdik.DDD.Web.FilePathProviders.Static;
using MartinDrozdik.DDD.Web.FilePathProviders.StaticResources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MartinDrozdik.DDD.Web.Tests.FilePathProviders.Static;

public class VersionedStaticFilePathProviderTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("/a/b/c")]
    [InlineData("/a/b/c.css")]
    [InlineData("yep.css")]
    [InlineData("/a/b/c.js")]
    [InlineData("a/b/c.js")]
    [InlineData("interesting.png")]
    public async Task Version_is_added(string path)
    {
        // Arrange
        const string Version = "1.2.3";
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<StaticFileVersioningOptions>(options => options.Version, Version)
            .WithServices(services => services.RemoveAll<IStaticFilePathProvider>())
            .WithServices(services => services.AddTransient<IStaticFilePathProvider, VersionedStaticFilePathProvider>())
            .Build();
        var service = factory.Services.GetRequiredService<IStaticFilePathProvider>();

        // Act
        var result = service.PathTo(path);

        // Assert
        Assert.Equal($"{path}?version={Version}", result);
    }
}
