using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Health;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class OutboxHealthCheckTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task An_empty_outbox_is_healthy()
    {
        // Arrange
        using var app = Build();

        // Act
        var report = await CheckAsync(app);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(0, report.Data["pending"]);
        Assert.Equal(0, report.Data["deadLettered"]);
    }

    [Fact]
    public async Task A_backlog_above_the_threshold_is_degraded()
    {
        // Arrange
        using var app = Build(degradedBacklogThreshold: 1);
        await EnqueueAsync(app, "one");
        await EnqueueAsync(app, "two");

        // Act
        var report = await CheckAsync(app);

        // Assert
        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Equal(2, report.Data["pending"]);
    }

    [Fact]
    public async Task A_dead_lettered_message_makes_the_outbox_unhealthy()
    {
        // Arrange
        using var app = Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, "doomed");
        state.ShouldFail = true;

        // Act
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var report = await CheckAsync(app);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(1, report.Data["deadLettered"]);
    }

    private static async Task<HealthReportEntry> CheckAsync(TestedApp<Program> app)
    {
        var service = app.Services.GetRequiredService<HealthCheckService>();
        var report = await service.CheckHealthAsync(
            registration => registration.Name == HealthChecksBuilderExtensions.DefaultName,
            TestContext.Current.CancellationToken);

        return report.Entries[HealthChecksBuilderExtensions.DefaultName];
    }

    private static async Task EnqueueAsync(TestedApp<Program> app, string text)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage(text));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private TestedApp<Program> Build(int degradedBacklogThreshold = 1000)
        => new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services =>
            {
                services.Configure<OutboxOptions>(options => options.RetryDelays = []);
                services.AddHealthChecks()
                        .AddOutboxHealthCheck<TestDbContext>(degradedBacklogThreshold);
            })
            .Build();
}
