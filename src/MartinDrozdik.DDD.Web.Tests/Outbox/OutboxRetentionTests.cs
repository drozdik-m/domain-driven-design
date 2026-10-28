using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Outbox.Retention;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies that <see cref="IOutboxRetention{TDbContext}"/> deletes old delivered messages, and only those, against a real database.
/// </summary>
/// <remarks>
/// The test application adds the retention to <see cref="TestDbContext"/> with a retention of seven days.
/// </remarks>
public class OutboxRetentionTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Retention_deletes_delivered_messages_and_keeps_everything_else()
    {
        // Arrange
        using var app = Build(out var time, out _, configureOutbox: outboxOptions => outboxOptions.RetryDelays = []);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();

        await EnqueueAsync(app, new TestOutboxMessage("delivered"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        state.ShouldFail = true;
        await EnqueueAsync(app, new TestOutboxMessage("dead"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Old enough for the delivered message to fall out of retention
        time.Advance(TimeSpan.FromDays(8));
        await EnqueueAsync(app, new TestOutboxMessage("fresh"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var deleted = await app.ApplyOutboxRetentionAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var remaining = await context.Set<OutboxMessage>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, deleted);

        // The dead-lettered message stays, because somebody has to look at it
        Assert.Contains(remaining, m => m.FailedAt is not null);

        // The message delivered eight days ago is gone, the one delivered just now is still here
        Assert.DoesNotContain(remaining, m => m.ProcessedAt == s_initialTime.UtcDateTime);
        Assert.Contains(remaining, m => m.ProcessedAt == time.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task Retention_keeps_delivered_messages_younger_than_the_retention()
    {
        // Arrange
        using var app = Build(out var time, out _);
        await EnqueueAsync(app, new TestOutboxMessage("kept"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromDays(6));
        var deleted = await app.ApplyOutboxRetentionAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, deleted);
        Assert.Equal(1, await CountMessagesAsync(app));
    }

    [Fact]
    public async Task Retention_deletes_a_backlog_chunk_by_chunk_in_one_call()
    {
        // Arrange
        using var app = Build(out var time, out var logger, retentionOptions => retentionOptions.BatchSize = 2);
        for (var i = 0; i < 5; i++)
        {
            await EnqueueAsync(app, new TestOutboxMessage($"old {i}"));
        }

        await app.DrainOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromDays(8));
        logger.Clear();

        // Act
        var deleted = await app.ApplyOutboxRetentionAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The whole backlog goes in one call, but never more than two rows per statement
        Assert.Equal(5, deleted);
        Assert.Equal(0, await CountMessagesAsync(app));

        var deletes = logger.From("Microsoft.EntityFrameworkCore.Database.Command")
            .Count(e => e.Message.Contains("DELETE FROM", StringComparison.Ordinal));
        Assert.Equal(3, deletes);
    }

    [Fact]
    public async Task Delivering_a_batch_leaves_old_delivered_messages_to_the_retention()
    {
        // Arrange
        // Commits wake the delivery often; the deletes must not ride along with every batch
        using var app = Build(out var time, out _);
        await EnqueueAsync(app, new TestOutboxMessage("old"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromDays(8));
        await EnqueueAsync(app, new TestOutboxMessage("fresh"));

        // Act
        var result = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Dispatched);
        Assert.Equal(2, await CountMessagesAsync(app));
    }

    [Fact]
    public async Task An_outbox_without_retention_keeps_its_delivered_messages()
    {
        // Arrange
        // The test application adds no retention to the second context
        using var app = Build(out _, out _);

        // Act
        using var scope = app.Services.CreateScope();
        var retention = scope.ServiceProvider.GetService<IOutboxRetention<SecondTestDbContext>>();

        // Assert
        Assert.Null(retention);
    }

    private static async Task EnqueueAsync<TMessage>(TestedApp<Program> app, TMessage message)
        where TMessage : IOutboxMessage
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> CountMessagesAsync(TestedApp<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Builds an application with a controllable clock, the retention of seven days of the test application and optional overrides.
    /// </summary>
    /// <param name="time">The clock every part of the outbox reads.</param>
    /// <param name="logger">The recorded log output.</param>
    /// <param name="configureRetention">Retention options to override, or null.</param>
    /// <param name="configureOutbox">Outbox options to override, or null.</param>
    /// <returns>The application under test.</returns>
    private TestedApp<Program> Build(
        out FakeTimeProvider time,
        out TestLogger logger,
        Action<OutboxRetentionOptions<TestDbContext>>? configureRetention = null,
        Action<OutboxOptions<TestDbContext>>? configureOutbox = null)
    {
        var fakeTime = new FakeTimeProvider(s_initialTime);
        time = fakeTime;

        var builder = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(fakeTime)
            .WithTestingLogger(out logger);

        if (configureRetention is not null)
        {
            builder.WithServices(services => services.Configure(configureRetention));
        }

        if (configureOutbox is not null)
        {
            builder.WithServices(services => services.Configure(configureOutbox));
        }

        return builder.Build();
    }
}
