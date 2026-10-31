using MartinDrozdik.DDD.Web.Redirects;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests.Redirects;

public class HostApplicationBuilderExtensionsTests
{
    [Fact]
    public void AddTemporaryRedirect_throws_for_a_path_that_already_redirects()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.AddPermanentRedirect("/contact.html", "/");

        // Act
        var exception = Record.Exception(() => builder.AddTemporaryRedirect("/Contact.html", "/contact"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Redirects_share_one_registry()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder
            .AddPermanentRedirect("/contact.html", "/")
            .AddPermanentRedirect("/about.html", "/")
            .AddTemporaryRedirect("/blog", "/news");

        // Assert
        Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(RedirectRegistry));
    }
}
