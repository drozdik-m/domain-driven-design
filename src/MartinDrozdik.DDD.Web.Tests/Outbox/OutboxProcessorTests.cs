using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies delivery, retries, dead-lettering, claiming and retention against a real database.
/// </summary>
public class OutboxProcessorTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Processing_delivers_a_pending_message_to_its_handler()
    {
        // Arrange
        using var app = Build(out var time);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("hello"));

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["hello"], state.Handled);

        var message = await GetSingleMessageAsync(app);
        Assert.Equal(time.GetUtcNow().UtcDateTime, message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.ClaimedBy);
    }

    [Fact]
    public async Task Two_message_types_are_routed_to_their_own_handlers()
    {
        // Arrange
        using var app = Build(out _);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("first"));
        await EnqueueAsync(app, new OtherTestOutboxMessage(42));

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, dispatched);
        Assert.Equal(["first", "other:42"], state.Handled);
    }

    [Fact]
    public async Task A_delivered_message_is_never_delivered_again()
    {
        // Arrange
        using var app = Build(out _);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("once"));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Single(state.Handled);
    }

    [Fact]
    public async Task A_failing_message_is_retried_after_each_configured_delay_and_then_dead_lettered()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.RetryDelays = [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)];
            },
            out var logger);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("doomed"));
        state.ShouldFail = true;

        // Act
        // First attempt fails, so the message waits ten seconds
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var afterFirst = await GetSingleMessageAsync(app);

        // Second attempt fails, so the message waits a minute
        time.Advance(TimeSpan.FromSeconds(10));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var afterSecond = await GetSingleMessageAsync(app);

        // Third attempt has no delay left, so the message is dead-lettered
        time.Advance(TimeSpan.FromMinutes(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var afterThird = await GetSingleMessageAsync(app);

        // Assert
        Assert.Equal(1, afterFirst.Attempts);
        Assert.Equal(s_initialTime.UtcDateTime.AddSeconds(10), afterFirst.AvailableAt);
        Assert.Null(afterFirst.FailedAt);

        Assert.Equal(2, afterSecond.Attempts);
        Assert.Equal(s_initialTime.UtcDateTime.AddSeconds(10).AddMinutes(1), afterSecond.AvailableAt);
        Assert.Null(afterSecond.FailedAt);

        Assert.Equal(3, afterThird.Attempts);
        Assert.NotNull(afterThird.FailedAt);
        Assert.Contains("Delivery failed on purpose.", afterThird.LastError, StringComparison.Ordinal);

        // Retries are warnings, the final give-up is an error
        Assert.Equal(2, logger.At(LogLevel.Warning).Count(e => e.Message.Contains("will be retried", StringComparison.Ordinal)));
        Assert.Single(logger.At(LogLevel.Error), e => e.Message.Contains("dead-lettered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_message_waiting_on_a_retry_delay_is_not_delivered_early()
    {
        // Arrange
        using var app = Build(out var time, options => options.RetryDelays = [TimeSpan.FromMinutes(5)]);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("later"));
        state.ShouldFail = true;
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Act
        time.Advance(TimeSpan.FromMinutes(4));
        var tooEarly = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(1));
        var onTime = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, tooEarly);
        Assert.Equal(1, onTime);
        Assert.Equal(["later"], state.Handled);
    }

    [Fact]
    public async Task A_dead_lettered_message_is_never_picked_up_again()
    {
        // Arrange
        using var app = Build(out var time, options => options.RetryDelays = []);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("doomed"));
        state.ShouldFail = true;
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Act
        time.Advance(TimeSpan.FromDays(1));
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Empty(state.Handled);
        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task An_empty_retry_list_dead_letters_on_the_first_failure()
    {
        // Arrange
        using var app = Build(out _, options => options.RetryDelays = []);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("doomed"));
        state.ShouldFail = true;

        // Act
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_message_of_an_unregistered_type_is_retried_rather_than_dead_lettered_at_once()
    {
        // A rolling deployment lets an old instance see a message type only the new one registers.
        // Dead-lettering on the spot would lose it, so an unknown type is retried like any other failure.

        // Arrange
        using var app = Build(out _, options => options.RetryDelays = [TimeSpan.FromMinutes(5)], out var logger);
        await AddRawMessageAsync(app, OutboxMessage.Create("test.not-registered.v1", "{}", s_initialTime.UtcDateTime));

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);

        var message = await GetSingleMessageAsync(app);
        Assert.Null(message.FailedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(s_initialTime.UtcDateTime.AddMinutes(5), message.AvailableAt);
        Assert.Contains("No handler is registered", message.LastError, StringComparison.Ordinal);

        Assert.DoesNotContain(logger.At(LogLevel.Error), e => e.Message.Contains("dead-lettered", StringComparison.Ordinal));

        // The unhandled type is called out on its own, so it stays distinguishable from a handler that threw
        Assert.Single(
            logger.At(LogLevel.Warning),
            e => e.Message.Contains("no registered handler", StringComparison.Ordinal));
        Assert.Single(
            logger.At(LogLevel.Warning),
            e => e.Exception?.Message.Contains("No handler is registered", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task A_message_of_an_unregistered_type_is_dead_lettered_once_the_retries_run_out()
    {
        // Arrange
        using var app = Build(out var time, options => options.RetryDelays = [TimeSpan.FromMinutes(5)], out var logger);
        await AddRawMessageAsync(app, OutboxMessage.Create("test.not-registered.v1", "{}", s_initialTime.UtcDateTime));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromMinutes(5));
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);

        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Equal(2, message.Attempts);
        Assert.Contains("No handler is registered", message.LastError, StringComparison.Ordinal);

        Assert.Single(
            logger.At(LogLevel.Error),
            e => e.Message.Contains("dead-lettered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_batch_never_delivers_more_than_the_configured_size()
    {
        // Arrange
        using var app = Build(out _, options => options.BatchSize = 2);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));
        await EnqueueAsync(app, new TestOutboxMessage("c"));

        // Act
        var first = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var second = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, first);
        Assert.Equal(1, second);
        Assert.Equal(["a", "b", "c"], state.Handled);
    }

    [Fact]
    public async Task A_message_claimed_by_another_processor_is_skipped()
    {
        // Arrange
        using var app = Build(out var time);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("contested"));

        // Another instance takes the message first, using a lease that has not expired
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
            message.Claim(Guid.CreateVersion7(), time.GetUtcNow().UtcDateTime, TimeSpan.FromMinutes(5));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Empty(state.Handled);
    }

    [Fact]
    public async Task A_message_whose_lease_has_expired_is_picked_up_again()
    {
        // Arrange
        using var app = Build(out var time, options => options.LeaseDuration = TimeSpan.FromMinutes(5));
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("abandoned"));

        // A processor that took the message and then died without releasing it
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
            message.Claim(Guid.CreateVersion7(), time.GetUtcNow().UtcDateTime, TimeSpan.FromMinutes(5));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        time.Advance(TimeSpan.FromMinutes(6));
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["abandoned"], state.Handled);
    }

    [Fact]
    public async Task Retention_deletes_delivered_messages_and_keeps_everything_else()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.Retention = TimeSpan.FromDays(7);
                options.RetryDelays = [];
            });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();

        await EnqueueAsync(app, new TestOutboxMessage("delivered"));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        state.ShouldFail = true;
        await EnqueueAsync(app, new TestOutboxMessage("dead"));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Act
        // Old enough for the delivered message to fall out of retention
        time.Advance(TimeSpan.FromDays(8));
        await EnqueueAsync(app, new TestOutboxMessage("fresh"));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var remaining = await context.Set<OutboxMessage>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        // The dead-lettered message stays, because somebody has to look at it
        Assert.Contains(remaining, m => m.FailedAt is not null);

        // The message delivered eight days ago is gone, the one delivered just now is still here
        Assert.DoesNotContain(remaining, m => m.ProcessedAt == s_initialTime.UtcDateTime);
        Assert.Contains(remaining, m => m.ProcessedAt == time.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task Retention_keeps_delivered_messages_forever_by_default()
    {
        // Arrange
        using var app = Build(out var time);
        await EnqueueAsync(app, new TestOutboxMessage("kept"));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromDays(3650));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.ProcessedAt);
    }

    /// <summary>
    /// Enqueues a message and commits it, exactly as application code would.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="app">The application under test.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <returns>A <see cref="Task"/> that completes when the message is committed.</returns>
    private static async Task EnqueueAsync<TMessage>(TestedApp<Program> app, TMessage message)
        where TMessage : IOutboxMessage
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Stores a message directly, bypassing the registration checks of the outbox.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <param name="message">The message to store.</param>
    /// <returns>A <see cref="Task"/> that completes when the message is committed.</returns>
    private static async Task AddRawMessageAsync(TestedApp<Program> app, OutboxMessage message)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads the only stored message, from a scope of its own so nothing is served from a change tracker.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <returns>The stored message.</returns>
    private static async Task<OutboxMessage> GetSingleMessageAsync(TestedApp<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
    }

    private TestedApp<Program> Build(out FakeTimeProvider time)
        => Build(out time, configureOptions: null);

    private TestedApp<Program> Build(out FakeTimeProvider time, Action<OutboxOptions>? configureOptions)
        => Build(out time, configureOptions, out _);

    /// <summary>
    /// Builds an application with a controllable clock and outbox options.
    /// </summary>
    /// <param name="time">The clock every part of the outbox reads.</param>
    /// <param name="configureOptions">Outbox options to override, or null for the defaults.</param>
    /// <param name="logger">The recorded log output.</param>
    /// <returns>The application under test.</returns>
    private TestedApp<Program> Build(
        out FakeTimeProvider time,
        Action<OutboxOptions>? configureOptions,
        out TestLogger logger)
    {
        var fakeTime = new FakeTimeProvider(s_initialTime);
        time = fakeTime;

        var builder = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(fakeTime)
            .WithTestingLogger(out logger);

        if (configureOptions is not null)
        {
            builder.WithServices(services => services.Configure(configureOptions));
        }

        return builder.Build();
    }
}
