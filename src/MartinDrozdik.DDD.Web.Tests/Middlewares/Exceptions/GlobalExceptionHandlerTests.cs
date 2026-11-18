using System.Text.Json;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.Middlewares.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares.Exceptions;

public class GlobalExceptionHandlerTests
{
    private const string Secret = "Login failed for user 'sa' on server db-prod-01";

    [Fact]
    public async Task Production_response_hides_the_message_of_an_unexpected_exception()
    {
        // Arrange
        var context = CreateTestContext();
        var handler = CreateHandler(Microsoft.Extensions.Hosting.Environments.Production);

        // Act
        await handler.TryHandleAsync(context, new InvalidOperationException(Secret), TestContext.Current.CancellationToken);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);
        Assert.Equal("An unexpected server error occurred.", problemDetails.Detail);
        Assert.DoesNotContain(Secret, await ReadBodyAsync(context), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Development_response_carries_the_message_of_an_unexpected_exception()
    {
        // Arrange
        var context = CreateTestContext();
        var handler = CreateHandler(Microsoft.Extensions.Hosting.Environments.Development);

        // Act
        await handler.TryHandleAsync(context, new InvalidOperationException(Secret), TestContext.Current.CancellationToken);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);
        Assert.Equal(Secret, problemDetails.Detail);
    }

    [Fact]
    public async Task Production_response_carries_the_message_of_a_business_rule_exception()
    {
        // Arrange
        var context = CreateTestContext();
        var handler = CreateHandler(Microsoft.Extensions.Hosting.Environments.Production);

        // Act
        await handler.TryHandleAsync(context, new BusinessRuleException("The invoice is already paid"), TestContext.Current.CancellationToken);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);
        Assert.Equal("The invoice is already paid", problemDetails.Detail);
    }

    private static GlobalExceptionHandler CreateHandler(string environmentName)
    {
        var environment = new TestHostingEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = nameof(GlobalExceptionHandlerTests),
            ContentRootPath = AppContext.BaseDirectory,
            ContentRootFileProvider = new NullFileProvider(),
        };

        return new GlobalExceptionHandler(environment, new TestLogger().For<GlobalExceptionHandler>());
    }

    private static DefaultHttpContext CreateTestContext()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .AddProblemDetails()
                .BuildServiceProvider(),
        };

        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpContext context)
    {
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(await ReadBodyAsync(context), JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        return problemDetails;
    }

    private sealed class TestHostingEnvironment : IHostEnvironment
    {
        public required string EnvironmentName { get; set; }

        public required string ApplicationName { get; set; }

        public required string ContentRootPath { get; set; }

        public required IFileProvider ContentRootFileProvider { get; set; }
    }
}
