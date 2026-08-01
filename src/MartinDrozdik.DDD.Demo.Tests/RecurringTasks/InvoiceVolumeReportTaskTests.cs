using MartinDrozdik.DDD.Demo.RecurringTasks;
using MartinDrozdik.DDD.Testing.RecurringTasks;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Demo.Tests.RecurringTasks;

public class InvoiceVolumeReportTaskTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Report_runs_against_the_database_and_logs_the_invoice_count()
    {
        // Arrange
        // No loop is started, the task is simply invoked once in a scope of its own
        using var app = new DemoAppBuilder(testOutputHelper)
            .WithTestingLogger(out var logger)
            .Build();

        // Act
        await app.RunRecurringTaskAsync<InvoiceVolumeReportTask>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            logger.At(LogLevel.Information),
            entry => entry.Message.Contains("invoices as of", StringComparison.Ordinal));
    }
}
