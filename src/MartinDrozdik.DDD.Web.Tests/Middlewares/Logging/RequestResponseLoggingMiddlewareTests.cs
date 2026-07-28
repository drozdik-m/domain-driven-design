using System.Net;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.Middlewares.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares.Logging;

public class RequestResponseLoggingMiddlewareTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)] // 200
    [InlineData(HttpStatusCode.Created)] // 201
    [InlineData(HttpStatusCode.NoContent)] // 204
    [InlineData(HttpStatusCode.MovedPermanently)] // 301
    [InlineData(HttpStatusCode.Found)] // 302
    [InlineData(HttpStatusCode.NotModified)] // 304
    [InlineData(HttpStatusCode.TemporaryRedirect)] // 307
    [InlineData(HttpStatusCode.PermanentRedirect)] // 308
    public async Task Successful_and_redirection_responses_are_logged_as_information(HttpStatusCode statusCode)
    {
        // Arrange
        var logger = new TestLogger();
        var middleware = CreateTestMiddleware(logger.For<RequestResponseLoggingMiddleware>(), statusCode);
        var context = CreateTestContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(LogLevel.Information, logger.Last.Level);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)] // 400
    [InlineData(HttpStatusCode.Unauthorized)] // 401
    [InlineData(HttpStatusCode.Forbidden)] // 403
    [InlineData(HttpStatusCode.NotFound)] // 404
    [InlineData(HttpStatusCode.Conflict)] // 409
    [InlineData(HttpStatusCode.UnprocessableEntity)] // 422
    [InlineData(HttpStatusCode.TooManyRequests)] // 429
    public async Task Client_error_responses_are_logged_as_warnings(HttpStatusCode statusCode)
    {
        // Arrange
        var logger = new TestLogger();
        var middleware = CreateTestMiddleware(logger.For<RequestResponseLoggingMiddleware>(), statusCode);
        var context = CreateTestContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(LogLevel.Warning, logger.Last.Level);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.BadGateway)] // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)] // 503
    public async Task Server_error_responses_are_logged_as_errors(HttpStatusCode statusCode)
    {
        // Arrange
        var logger = new TestLogger();
        var middleware = CreateTestMiddleware(logger.For<RequestResponseLoggingMiddleware>(), statusCode);
        var context = CreateTestContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(LogLevel.Error, logger.Last.Level);
    }

    [Fact]
    public async Task Thrown_exception_is_logged_as_error_and_rethrown()
    {
        // Arrange
        var logger = new TestLogger();
        var expectedException = new InvalidOperationException("Boom");
        var middleware = new RequestResponseLoggingMiddleware(_ => throw expectedException, logger.For<RequestResponseLoggingMiddleware>());
        var context = CreateTestContext();

        // Act
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        // Assert
        var responseEntry = logger.Last;
        Assert.Same(expectedException, thrownException);
        Assert.Equal(LogLevel.Error, responseEntry.Level);
        Assert.Same(expectedException, responseEntry.Exception);
    }

    [Fact]
    public async Task Incoming_request_is_logged_as_information_before_the_response()
    {
        // Arrange
        var logger = new TestLogger();
        var middleware = CreateTestMiddleware(logger.For<RequestResponseLoggingMiddleware>(), HttpStatusCode.InternalServerError);
        var context = CreateTestContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(2, logger.Entries.Count);
        Assert.Equal(LogLevel.Information, logger.Entries[0].Level);
        Assert.Contains("https://localhost/test?id=123", logger.Entries[0].Message, StringComparison.Ordinal);
    }

    private static RequestResponseLoggingMiddleware CreateTestMiddleware(
        ILogger<RequestResponseLoggingMiddleware> logger,
        HttpStatusCode statusCode)
    {
        return new RequestResponseLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            },
            logger);
    }

    private static DefaultHttpContext CreateTestContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/test";
        context.Request.QueryString = new QueryString("?id=123");
        return context;
    }
}
