using MartinDrozdik.DDD.Web.Redirects;
using Microsoft.AspNetCore.Http;

namespace MartinDrozdik.DDD.Web.Tests.Redirects;

public class RedirectTests
{
    [Theory]
    [InlineData("contact.html")]
    [InlineData("//example.com/contact.html")]
    [InlineData("/contact.html?id=1")]
    [InlineData("/contact.html#form")]
    [InlineData(" ")]
    public void Constructor_throws_for_an_invalid_source(string from)
    {
        // Arrange
        // Act
        var exception = Record.Exception(() => new Redirect(from, "/"));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData("about")]
    [InlineData("//example.com/")]
    [InlineData("ftp://example.com/")]
    [InlineData("javascript:alert(1)")]
    [InlineData(" ")]
    public void Constructor_throws_for_an_invalid_target(string to)
    {
        // Arrange
        // Act
        var exception = Record.Exception(() => new Redirect("/contact.html", to));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData("/contact.html", "/contact.html")]
    [InlineData("/contact.html", "/CONTACT.html?from=old")]
    public void Constructor_throws_for_a_redirect_to_itself(string from, string to)
    {
        // Arrange
        // Act
        var exception = Record.Exception(() => new Redirect(from, to));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void BuildLocationHeader_prefixes_the_path_base()
    {
        // Arrange
        var redirect = new Redirect("/contact.html", "/");
        var request = CreateRequest("/app", "/contact.html", string.Empty);

        // Act
        var location = redirect.BuildLocationHeader(request);

        // Assert
        Assert.Equal("/app/", location);
    }

    [Fact]
    public void BuildLocationHeader_encodes_a_non_ascii_target()
    {
        // Arrange
        var redirect = new Redirect("/about.html", "/o-nás");
        var request = CreateRequest(string.Empty, "/about.html", string.Empty);

        // Act
        var location = redirect.BuildLocationHeader(request);

        // Assert
        Assert.Equal("/o-n%C3%A1s", location);
    }

    [Fact]
    public void BuildLocationHeader_carries_the_query_string_over_to_an_absolute_target()
    {
        // Arrange
        var redirect = new Redirect("/blog", "https://blog.example.com/posts");
        var request = CreateRequest(string.Empty, "/blog", "?utm_source=partner");

        // Act
        var location = redirect.BuildLocationHeader(request);

        // Assert
        Assert.Equal("https://blog.example.com/posts?utm_source=partner", location);
    }

    [Fact]
    public void BuildLocationHeader_keeps_the_query_string_of_an_absolute_target()
    {
        // Arrange
        var redirect = new Redirect("/blog", "https://blog.example.com/?from=old");
        var request = CreateRequest(string.Empty, "/blog", "?utm_source=partner");

        // Act
        var location = redirect.BuildLocationHeader(request);

        // Assert
        Assert.Equal("https://blog.example.com/?from=old", location);
    }

    private static HttpRequest CreateRequest(string pathBase, string path, string query)
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = new PathString(pathBase);
        context.Request.Path = new PathString(path);
        context.Request.QueryString = new QueryString(query);
        return context.Request;
    }
}
