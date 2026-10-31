using System.Net;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Redirects;

public class RedirectMiddlewareTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Permanent_redirect_answers_301_with_the_new_location()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/contact.html", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Temporary_redirect_answers_302_with_an_absolute_location()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://blog.example.com/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Redirect_matches_the_path_ignoring_case()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/CONTACT.HTML", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
    }

    [Fact]
    public async Task Redirect_carries_over_the_query_string_when_the_target_has_none()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/contact.html?utm_source=partner", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/?utm_source=partner", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Redirect_matches_a_percent_encoded_path_and_keeps_the_query_and_fragment_of_the_target()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/o-n%C3%A1s.html?utm_source=partner", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/about?from=old#team", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Path_without_a_redirect_reaches_the_application()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient CreateClient(TestedApp<Program> app)
    {
        return app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }
}
