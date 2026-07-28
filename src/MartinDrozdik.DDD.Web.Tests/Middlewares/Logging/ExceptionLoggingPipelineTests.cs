using System.Collections.Immutable;
using System.Net;
using MartinDrozdik.DDD.Testing.Logging;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.Middlewares.Logging;

/// <summary>
/// Verifies how the middleware pipeline composed by <see cref="WebApplicationExtensions.UseAppMiddlewares(Microsoft.AspNetCore.Builder.WebApplication)"/>
/// logs exceptions that are mapped to a response by an exception handler.
/// </summary>
public class ExceptionLoggingPipelineTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Exception_mapped_to_404_is_never_logged_as_an_error()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithTestingLogger(out var testingLogger)
            .Build();

        // Act
        var response = await app.CreateClient().GetAsync("/throw/not-found", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(RequestEntries(testingLogger), entry => entry.Level >= LogLevel.Error);
        Assert.Contains(
            RequestEntries(testingLogger),
            entry => entry.Level == LogLevel.Warning && entry.Message.Contains("=> 404", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Exception_mapped_to_500_is_logged_as_an_error()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithTestingLogger(out var testingLogger)
            .Build();

        // Act
        var response = await app.CreateClient().GetAsync("/throw/unhandled", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(
            RequestEntries(testingLogger),
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("=> 500", StringComparison.Ordinal));
    }

    /// <summary>
    /// Filters out framework logging unrelated to the request pipeline under test.
    /// </summary>
    /// <param name="testingLogger">The logger the application logged into.</param>
    /// <returns>The entries logged by this library.</returns>
    private static ImmutableList<LogEntry> RequestEntries(TestLogger testingLogger)
    {
        return testingLogger.From("MartinDrozdik");
    }
}
