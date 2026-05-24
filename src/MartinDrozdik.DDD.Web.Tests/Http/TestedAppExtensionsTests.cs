using MartinDrozdik.DDD.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit.Sdk;

namespace MartinDrozdik.DDD.Web.Tests.Http;

public class TestedAppExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task GET_JSON_works_correctly()
    {
        // Arrange
        const string url = "/api/test/get";
        var foo = new FooResponse();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () => foo);
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<FooResponse>(url);

        // Assert
        await response.EnsureSuccessAsync();
        Assert.Equal(foo.Id, response.Value.Id);
    }

    [Fact]
    public async Task GET_JSON_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/get";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<FooResponse>(url);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task GET_JSON_response_contains_correct_status_code()
    {
        // Arrange
        const string url = "/api/test/get";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () => Results.NotFound());
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<FooResponse>(url);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.Response.StatusCode);
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
        Assert.True(response.IsSuccess);
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
                config.MapDelete(url, () => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.DeleteAsync(url);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task DELETE_JSON_with_model_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        var foo = new FooResponse();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () => foo);
            })
            .Build();

        // Act
        var response = await factory.DeleteJsonAsync<FooResponse>(url);

        // Assert
        await response.EnsureSuccessAsync();
        Assert.Equal(foo.Id, response.Value.Id);
    }

    [Fact]
    public async Task DELETE_JSON_with_model_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/delete";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapDelete(url, () => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.DeleteJsonAsync<FooResponse>(url);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task POST_JSON_works_correctly()
    {
        // Arrange
        const string url = "/api/test/post";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPost(url, (FooPayload e) => Assert.Equal(payload.Name, e.Name));
            })
            .Build();

        // Act
        var response = await factory.PostJsonAsync(url, payload);

        // Assert
        Assert.True(response.IsSuccess);
        await response.EnsureSuccessAsync();
    }

    [Fact]
    public async Task POST_JSON_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/post";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPost(url, (FooPayload _) => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.PostJsonAsync(url, payload);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task POST_JSON_with_response_works_correctly()
    {
        // Arrange
        const string url = "/api/test/post";
        var payload = new FooPayload { Name = "test" };
        var foo = new FooResponse();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPost(url, (FooPayload e) =>
                {
                    Assert.Equal(payload.Name, e.Name);
                    return foo;
                });
            })
            .Build();

        // Act
        var response = await factory.PostJsonWithResponseAsync<FooPayload, FooResponse>(url, payload);

        // Assert
        await response.EnsureSuccessAsync();
        Assert.Equal(foo.Id, response.Value.Id);
    }

    [Fact]
    public async Task POST_JSON_with_response_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/post";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPost(url, (FooPayload _) => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.PostJsonWithResponseAsync<FooPayload, FooResponse>(url, payload);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task PUT_JSON_works_correctly()
    {
        // Arrange
        const string url = "/api/test/put";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPut(url, (FooPayload e) => Assert.Equal(payload.Name, e.Name));
            })
            .Build();

        // Act
        var response = await factory.PutJsonAsync(url, payload);

        // Assert
        Assert.True(response.IsSuccess);
        await response.EnsureSuccessAsync();
    }

    [Fact]
    public async Task PUT_JSON_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/put";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPut(url, (FooPayload _) => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.PutJsonAsync(url, payload);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task PUT_JSON_with_response_works_correctly()
    {
        // Arrange
        const string url = "/api/test/put";
        var payload = new FooPayload { Name = "test" };
        var foo = new FooResponse();
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPut(url, (FooPayload e) =>
                {
                    Assert.Equal(payload.Name, e.Name);
                    return foo;
                });
            })
            .Build();

        // Act
        var response = await factory.PutJsonWithResponseAsync<FooPayload, FooResponse>(url, payload);

        // Assert
        await response.EnsureSuccessAsync();
        Assert.Equal(foo.Id, response.Value.Id);
    }

    [Fact]
    public async Task PUT_JSON_with_response_fail_works_correctly()
    {
        // Arrange
        const string url = "/api/test/put";
        var payload = new FooPayload { Name = "test" };
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapPut(url, (FooPayload _) => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.PutJsonWithResponseAsync<FooPayload, FooResponse>(url, payload);

        // Assert
        Assert.False(response.IsSuccess);
        await Assert.ThrowsAsync<FailException>(response.EnsureSuccessAsync);
    }

    [Fact]
    public async Task RequestResult_Value_throws_when_request_failed()
    {
        // Arrange
        const string url = "/api/test/get";
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithEndpoints(config =>
            {
                config.MapGet(url, () => Results.StatusCode(500));
            })
            .Build();

        // Act
        var response = await factory.GetJsonAsync<FooResponse>(url);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => response.Value);
    }

    private class FooResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    private class FooPayload
    {
        public string Name { get; set; } = string.Empty;
    }
}
