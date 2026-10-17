using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Health;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class OutboxHealthCheckTests(ITestOutputHelper testOutputHelper)
{
    private const string FirstName = $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{nameof(TestDbContext)}";
    private const string SecondName = $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{nameof(SecondTestDbContext)}";

    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_empty_outbox_is_healthy()
    {
        // Arrange
        using var app = Build();

        // Act
        var report = await CheckAsync(app, FirstName);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(0, report.Data["pending"]);
        Assert.Equal(0, report.Data["overdue"]);
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
        var report = await CheckAsync(app, FirstName);

        // Assert
        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Equal(2, report.Data["pending"]);
        Assert.Equal(2, report.Data["overdue"]);
    }

    [Fact]
    public async Task Messages_scheduled_for_later_do_not_degrade_the_outbox()
    {
        // Arrange
        // Nothing is late about a message scheduled for tomorrow, however many of them there are
        using var app = Build(degradedBacklogThreshold: 1);
        await ScheduleAsync(app, "tomorrow", s_initialTime.AddDays(1));
        await ScheduleAsync(app, "next week", s_initialTime.AddDays(7));

        // Act
        var report = await CheckAsync(app, FirstName);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(2, report.Data["pending"]);
        Assert.Equal(0, report.Data["overdue"]);
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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var report = await CheckAsync(app, FirstName);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(1, report.Data["deadLettered"]);
        Assert.Contains(nameof(TestDbContext), report.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_outbox_reports_its_own_health_under_a_name_of_its_own()
    {
        // Arrange
        using var app = Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, "doomed");
        state.ShouldFail = true;
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var first = await CheckAsync(app, FirstName);
        var second = await CheckAsync(app, SecondName);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, first.Status);
        Assert.Equal(HealthStatus.Healthy, second.Status);
        Assert.Equal(0, second.Data["deadLettered"]);
        Assert.Contains("ready", first.Tags);
        Assert.Contains("ready", second.Tags);
    }

    [Fact]
    public async Task An_outbox_can_be_reported_under_a_name_and_tags_of_your_choice()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.AddHealthChecks()
                .AddOutboxHealthCheck<TestDbContext>("custom-outbox", ["custom"], degradedBacklogThreshold: 5))
            .Build();

        // Act
        var report = await CheckAsync(app, "custom-outbox");

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal("custom", Assert.Single(report.Tags));
    }

    private static async Task<HealthReportEntry> CheckAsync(TestedApp<Program> app, string name)
    {
        var service = app.Services.GetRequiredService<HealthCheckService>();
        var report = await service.CheckHealthAsync(
            registration => registration.Name == name,
            TestContext.Current.CancellationToken);

        return report.Entries[name];
    }

    private static async Task EnqueueAsync(TestedApp<Program> app, string text)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage(text));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task ScheduleAsync(TestedApp<Program> app, string text, DateTimeOffset availableAt)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddNowAsync(
            new TestOutboxMessage(text),
            new OutboxMessageSettings { AvailableAt = availableAt },
            TestContext.Current.CancellationToken);
    }

    private TestedApp<Program> Build(int degradedBacklogThreshold = 1000)
        => new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(new FakeTimeProvider(s_initialTime))
            .WithServices(services =>
            {
                services.Configure<OutboxOptions<TestDbContext>>(options => options.RetryDelays = []);
                services.AddHealthChecks()
                        .AddOutboxHealthCheck<TestDbContext>(degradedBacklogThreshold)
                        .AddOutboxHealthCheck<SecondTestDbContext>(degradedBacklogThreshold);
            })
            .Build();
}
