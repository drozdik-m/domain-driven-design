using MartinDrozdik.DDD.Web.FilePathProviders.Static;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MartinDrozdik.DDD.Web.Tests.FilePathProviders.Static;

public class IdentityStaticFilePathProviderTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("/a/b/c")]
    [InlineData("/a/b/c.css")]
    [InlineData("yep.css")]
    [InlineData("/a/b/c.js")]
    [InlineData("a/b/c.js")]
    [InlineData("interesting.png")]
    public async Task Identical_paths_are_returned(string path)
    {
        // Arrange
        var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.RemoveAll<IStaticFilePathProvider>())
            .WithServices(services => services.AddSingleton<IStaticFilePathProvider, IdentityStaticFilePathProvider>())
            .Build();
        var service = factory.Services.GetRequiredService<IStaticFilePathProvider>();

        // Act
        var result = service.PathTo(path);

        // Assert
        Assert.Equal(path, result);
    }
}
