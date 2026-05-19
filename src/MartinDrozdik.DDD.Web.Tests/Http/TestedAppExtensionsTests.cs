using MartinDrozdik.DDD.Testing;
using Microsoft.AspNetCore.Builder;
using Xunit.Sdk;

namespace MartinDrozdik.DDD.Web.Tests.Http;

public class TestedAppExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task GET_JSON_works_correctly()
    {
        // Arrange
        const string url = "/api/test/get";
        var foo = new Foo();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () => foo);
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<Foo>(url);

        // Assert
        await response.EnsureSuccessAsync();
        var model = response.Value;
        Assert.Equal(foo.Id, model.Id);
    }

    [Fact]
    public async Task GET_JSON_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/get";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () =>
                {
                    throw new InvalidOperationException();
                });
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<Foo>(url);

        // Assert
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task DELETE_JSON_with_model_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        var foo = new Foo();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () => foo);
            })
            .Build();

        // Act
        var response = await factory.DeleteJsonAsync<Foo>(url);

        // Assert
        await response.EnsureSuccessAsync();
        var model = response.Value;
        Assert.Equal(foo.Id, model.Id);
    }

    [Fact]
    public async Task DELETE_JSON_with_model_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () =>
                {
                    throw new InvalidOperationException();
                });
            })
            .Build();

        // Act
        var response = await factory.DeleteJsonAsync<Foo>(url);

        // Assert
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task DELETE_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () => { });
            })
            .Build();

        // Act
        var response = await factory.DeleteAsync(url);

        // Assert
        await response.EnsureSuccessAsync();
    }

    [Fact]
    public async Task DELETE_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () =>
                {
                    throw new InvalidOperationException();
                });
            })
            .Build();

        // Act
        var response = await factory.DeleteAsync(url);

        // Assert
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    private class Foo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
