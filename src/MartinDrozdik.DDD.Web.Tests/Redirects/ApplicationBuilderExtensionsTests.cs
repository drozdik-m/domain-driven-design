using System.Net;
using MartinDrozdik.DDD.Web.Redirects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace MartinDrozdik.DDD.Web.Tests.Redirects;

public class ApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseRedirects_does_nothing_without_redirects()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseRedirects();
        app.MapGet("/contact.html", () => "Contact");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        // Act
        using var response = await client.GetAsync("/contact.html", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Middleware_added_before_UseRedirects_answers_first()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddPermanentRedirect("/contact.html", "/");
        await using var app = builder.Build();
        app.Use((HttpContext context, RequestDelegate _) =>
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            return Task.CompletedTask;
        });
        app.UseRedirects();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        // Act
        using var response = await client.GetAsync("/contact.html", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpStatusCode)StatusCodes.Status418ImATeapot, response.StatusCode);
    }
}
