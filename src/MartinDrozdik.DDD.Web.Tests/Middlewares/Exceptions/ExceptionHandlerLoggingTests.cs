using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.Middlewares.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentValidationException = FluentValidation.ValidationException;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares.Exceptions;

public class ExceptionHandlerLoggingTests
{
    [Fact]
    public async Task BusinessNotFoundExceptionHandler_logs_its_404_as_a_warning()
    {
        // Arrange
        var logger = new TestLogger();
        var handler = new BusinessNotFoundExceptionHandler(CreateTestEnvironment(), logger.For<BusinessNotFoundExceptionHandler>());
        var context = CreateTestContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new BusinessNotFoundException("Missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(LogLevel.Warning, logger.Last.Level);
    }

    [Fact]
    public async Task BusinessRuleValidationExceptionHandler_logs_its_400_as_a_warning()
    {
        // Arrange
        var logger = new TestLogger();
        var handler = new BusinessRuleValidationExceptionHandler(CreateTestEnvironment(), logger.For<BusinessRuleValidationExceptionHandler>());
        var context = CreateTestContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new BusinessRuleValidationException("Invalid"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(LogLevel.Warning, logger.Last.Level);
    }

    [Fact]
    public async Task ValidationExceptionHandler_logs_its_400_as_a_warning()
    {
        // Arrange
        var logger = new TestLogger();
        var handler = new ValidationExceptionHandler(CreateTestEnvironment(), logger.For<ValidationExceptionHandler>());
        var context = CreateTestContext();
        var exception = new FluentValidationException("Invalid", []);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(LogLevel.Warning, logger.Last.Level);
    }

    [Fact]
    public async Task GlobalExceptionHandler_logs_its_500_as_an_error()
    {
        // Arrange
        var logger = new TestLogger();
        var handler = new GlobalExceptionHandler(CreateTestEnvironment(), logger.For<GlobalExceptionHandler>());
        var context = CreateTestContext();
        var exception = new InvalidOperationException("Boom");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(LogLevel.Error, logger.Last.Level);
        Assert.Same(exception, logger.Last.Exception);
    }

    [Fact]
    public async Task Handlers_log_the_status_code_that_was_actually_written()
    {
        // Arrange
        var logger = new TestLogger();
        var handler = new BusinessNotFoundExceptionHandler(CreateTestEnvironment(), logger.For<BusinessNotFoundExceptionHandler>());
        var context = CreateTestContext();

        // Act
        await handler.TryHandleAsync(context, new BusinessNotFoundException("Missing"), TestContext.Current.CancellationToken);

        // Assert
        // The response is written before logging, so the log must not report the initial 200
        Assert.Contains("=> 404", logger.Last.Message, StringComparison.Ordinal);
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

        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static TestHostingEnvironment CreateTestEnvironment()
    {
        return new TestHostingEnvironment
        {
            EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production,
            ApplicationName = nameof(ExceptionHandlerLoggingTests),
            ContentRootPath = AppContext.BaseDirectory,
            ContentRootFileProvider = new NullFileProvider(),
        };
    }

    private sealed class TestHostingEnvironment : IHostEnvironment
    {
        public required string EnvironmentName { get; set; }

        public required string ApplicationName { get; set; }

        public required string ContentRootPath { get; set; }

        public required IFileProvider ContentRootFileProvider { get; set; }
    }
}
