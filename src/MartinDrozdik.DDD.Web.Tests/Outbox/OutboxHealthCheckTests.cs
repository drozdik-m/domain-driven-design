using System.Data.Common;
using System.Net;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Health;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class OutboxHealthCheckTests(ITestOutputHelper testOutputHelper)
{
    private static readonly string s_firstName = $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{typeof(TestDbContext).FullName}";
    private static readonly string s_secondName = $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{typeof(SecondTestDbContext).FullName}";

    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_empty_outbox_is_healthy()
    {
        // Arrange
        using var app = Build();

        // Act
        var report = await CheckAsync(app, s_firstName);

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
        var report = await CheckAsync(app, s_firstName);

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
        var report = await CheckAsync(app, s_firstName);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(2, report.Data["pending"]);
        Assert.Equal(0, report.Data["overdue"]);
    }

    [Fact]
    public async Task A_dead_lettered_message_degrades_the_outbox_but_never_makes_it_unhealthy()
    {
        // Arrange
        using var app = Build();
        await DeadLetterAsync(app, "doomed");

        // Act
        var report = await CheckAsync(app, s_firstName);

        // Assert
        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Equal(1, report.Data["deadLettered"]);
        Assert.Contains(typeof(TestDbContext).FullName!, report.Description, StringComparison.Ordinal);
        Assert.Contains("dead-lettered", report.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_dead_lettered_message_keeps_the_instance_ready_for_traffic()
    {
        // Arrange
        // The table is shared, so a not-ready outbox would take every instance out of load balancing at once,
        // and keep them out for as long as the dead letter is kept - which is until somebody deletes it
        using var app = Build();
        await DeadLetterAsync(app, "doomed");

        // Act
        var response = await app.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(nameof(HealthStatus.Degraded), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dead_letters_and_a_backlog_are_both_named_in_the_description()
    {
        // Arrange
        using var app = Build(degradedBacklogThreshold: 1);
        await DeadLetterAsync(app, "doomed");
        await EnqueueAsync(app, "one");
        await EnqueueAsync(app, "two");

        // Act
        var report = await CheckAsync(app, s_firstName);

        // Assert
        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Contains("1 dead-lettered", report.Description, StringComparison.Ordinal);
        Assert.Contains("2 overdue", report.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_outbox_whose_table_cannot_be_read_is_unhealthy()
    {
        // Arrange
        using var app = Build();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.ExecuteSqlRawAsync("DROP TABLE \"OutboxMessage\"", TestContext.Current.CancellationToken);
        }

        // Act
        var report = await CheckAsync(app, s_firstName);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.NotNull(report.Exception);
    }

    [Fact]
    public async Task Every_outbox_reports_its_own_health_under_a_name_of_its_own()
    {
        // Arrange
        using var app = Build();
        await DeadLetterAsync(app, "doomed");

        // Act
        var first = await CheckAsync(app, s_firstName);
        var second = await CheckAsync(app, s_secondName);

        // Assert
        Assert.Equal(HealthStatus.Degraded, first.Status);
        Assert.Equal(HealthStatus.Healthy, second.Status);
        Assert.Equal(0, second.Data["deadLettered"]);
        Assert.Contains("ready", first.Tags);
        Assert.Contains("ready", second.Tags);
    }

    [Fact]
    public void Contexts_with_the_same_class_name_in_different_namespaces_get_names_of_their_own()
    {
        // Arrange
        // Two modules may both call their context ModuleDbContext, and the health check service rejects a name used twice
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
                .AddOutboxHealthCheck<Billing.ModuleDbContext>()
                .AddOutboxHealthCheck<Shipping.ModuleDbContext>();
        using var provider = services.BuildServiceProvider();

        // Act
        var exception = Record.Exception(provider.GetRequiredService<HealthCheckService>);

        // Assert
        Assert.Null(exception);
        var names = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(r => r.Name);
        Assert.Equal(
            [
                $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{typeof(Billing.ModuleDbContext).FullName}",
                $"{HealthChecksBuilderExtensions.DefaultNamePrefix}-{typeof(Shipping.ModuleDbContext).FullName}",
            ],
            names);
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

    [Fact]
    public async Task A_probe_counts_in_one_query_that_seeks_past_the_delivered_messages()
    {
        // Arrange
        // Without retention the delivered messages pile up, and every readiness probe of every instance runs this query
        var cancellationToken = TestContext.Current.CancellationToken;
        var commands = new CommandRecorder();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commands)
            .Options;
        await using var context = new TestDbContext(options, TimeProvider.System);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var now = s_initialTime.UtcDateTime;
        var delivered = CreateMessage(now);
        delivered.Claim(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(1));
        delivered.MarkProcessed(now);
        var deadLettered = CreateMessage(now);
        deadLettered.Claim(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(1));
        deadLettered.MarkFailed(now, "boom");
        context.Set<OutboxMessage>().AddRange(delivered, deadLettered, CreateMessage(now), CreateMessage(now.AddDays(1)));
        await context.SaveChangesAsync(cancellationToken);

        var check = new OutboxHealthCheck<TestDbContext>(context, degradedBacklogThreshold: 1000, new FakeTimeProvider(s_initialTime));
        commands.Executed.Clear();

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        // Assert
        var query = Assert.Single(commands.Executed);
        var plan = await ExplainAsync(connection, query, cancellationToken);
        Assert.Contains(plan, step => step.StartsWith("SEARCH", StringComparison.Ordinal) && step.Contains("IX_OutboxMessage_ProcessedAt_FailedAt_AvailableAt", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, step => step.StartsWith("SCAN", StringComparison.Ordinal));
        Assert.Equal(1, result.Data["deadLettered"]);
        Assert.Equal(2, result.Data["pending"]);
        Assert.Equal(1, result.Data["overdue"]);
    }

    private static OutboxMessage CreateMessage(DateTime availableAtUtc)
        => OutboxMessage.Create(TestOutboxMessage.MessageType, """{"Text":"counted"}""", availableAtUtc, availableAtUtc);

    /// <summary>
    /// Asks SQLite how it would run a recorded command.
    /// </summary>
    /// <param name="connection">The open connection that holds the database.</param>
    /// <param name="command">The recorded command.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The steps of the query plan.</returns>
    private static async Task<IReadOnlyList<string>> ExplainAsync(SqliteConnection connection, RecordedCommand command, CancellationToken cancellationToken)
    {
        await using var explain = connection.CreateCommand();
        explain.CommandText = $"EXPLAIN QUERY PLAN {command.Text}";
        foreach (var (name, value) in command.Parameters)
        {
            explain.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        var steps = new List<string>();
        await using var reader = await explain.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(reader.GetString(reader.GetOrdinal("detail")));
        }

        return steps;
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
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(context, new TestOutboxMessage(text));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task DeadLetterAsync(TestedApp<Program> app, string text)
    {
        // No retries are configured, so the first failure dead-letters it
        await EnqueueAsync(app, text);
        app.Services.GetRequiredService<TestOutboxHandlerState>().ShouldFail = true;
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
    }

    private static async Task ScheduleAsync(TestedApp<Program> app, string text, DateTimeOffset availableAt)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(
            context,
            new TestOutboxMessage(text),
            new OutboxMessageSettings { AvailableAt = availableAt });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
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

    /// <summary>
    /// A command as it was sent to the database, kept after the command itself is disposed.
    /// </summary>
    /// <param name="Text">The SQL text.</param>
    /// <param name="Parameters">The names and values of its parameters.</param>
    private sealed record RecordedCommand(string Text, IReadOnlyList<(string Name, object? Value)> Parameters);

    /// <summary>
    /// Records every query a context sends to the database.
    /// </summary>
    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Executed { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command)
            => Executed.Add(new RecordedCommand(
                command.CommandText,
                [.. command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, p.Value))]));
    }
}
