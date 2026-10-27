using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies that one iteration of <see cref="OutboxDispatchRecurringTask{TDbContext}"/> drains a backlog batch after batch.
/// </summary>
public class OutboxDispatchRecurringTaskTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(s_initialTime);

    [Fact]
    public async Task One_iteration_drains_a_backlog_larger_than_a_batch()
    {
        // Arrange
        using var app = Build(options => options.BatchSize = 10);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, 25);

        // Act
        await RunIterationAsync(app, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(25, state.Handled.Count);
        Assert.All(await GetMessagesAsync(app), m => Assert.NotNull(m.ProcessedAt));
    }

    [Fact]
    public async Task A_full_batch_of_failures_does_not_stop_the_drain()
    {
        // Arrange
        // Failed messages wait for their retry delay, so the next batch takes the ones behind them
        using var app = Build(options =>
        {
            options.BatchSize = 2;
            options.RetryDelays = [TimeSpan.FromMinutes(1)];
        });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        state.ShouldFail = true;
        await EnqueueAsync(app, 3);

        // Act
        await RunIterationAsync(app, TestContext.Current.CancellationToken);

        // Assert
        // Every message got its first attempt in this one iteration
        var messages = await GetMessagesAsync(app);
        Assert.Equal(3, messages.Count);
        Assert.All(messages, m => Assert.Equal(1, m.Attempts));
        Assert.All(messages, m => Assert.NotNull(m.LastError));
    }

    [Fact]
    public async Task A_cancelled_iteration_stops_draining_between_batches()
    {
        // Arrange
        using var app = Build(options => options.BatchSize = 1);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, 3);

        using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        state.OnHandled = _ => iteration.Cancel();

        // Act
        await RunIterationAsync(app, iteration.Token);

        // Assert
        // More messages were waiting, but the cancellation ended the iteration before the next batch
        Assert.Single(state.Handled);
        Assert.Equal(2, (await GetMessagesAsync(app)).Count(m => m.ProcessedAt is null));
    }

    [Fact]
    public async Task A_message_enqueued_during_an_iteration_is_delivered_in_the_same_iteration()
    {
        // Arrange
        using var app = Build(options => options.BatchSize = 10);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, 3);

        // Enqueued while a partial batch is delivered, so only a further batch finds it
        var enqueued = false;
        state.OnHandled = _ =>
        {
            if (enqueued)
            {
                return;
            }

            enqueued = true;
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage("late"));
            scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChanges();
        };

        // Act
        await RunIterationAsync(app, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, state.Handled.Count);
        Assert.Contains("late", state.Handled);
    }

    [Fact]
    public async Task The_rest_of_a_slow_batch_is_delivered_in_the_same_iteration()
    {
        // Arrange
        // The first message outlasts the reservation of the others, so the batch releases them unstarted
        using var app = Build(options => options.BatchSize = 10);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, 3);

        var slow = true;
        state.OnHandled = _ =>
        {
            if (slow)
            {
                slow = false;
                _time.Advance(TimeSpan.FromMinutes(2));
            }
        };

        // Act
        await RunIterationAsync(app, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, state.Handled.Count);
        Assert.Equal(3, state.Handled.Distinct(StringComparer.Ordinal).Count());
        Assert.All(await GetMessagesAsync(app), m => Assert.NotNull(m.ProcessedAt));
    }

    /// <summary>
    /// Runs one iteration of the dispatch task in a scope of its own, as the recurring task host does.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <param name="cancellationToken">Cancelled to end the iteration.</param>
    /// <returns>A <see cref="Task"/> that completes when the iteration is over.</returns>
    private static async Task RunIterationAsync(TestedApp<Program> app, CancellationToken cancellationToken)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var task = scope.ServiceProvider.GetRequiredService<OutboxDispatchRecurringTask<TestDbContext>>();
        await task.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Enqueues messages and commits them, exactly as application code would.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <param name="count">How many messages to enqueue.</param>
    /// <returns>A <see cref="Task"/> that completes when the messages are committed.</returns>
    private static async Task EnqueueAsync(TestedApp<Program> app, int count)
    {
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        for (var i = 0; i < count; i++)
        {
            outbox.AddOnSave(new TestOutboxMessage($"message-{i}"));
        }

        await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads every stored message, from a scope of its own so nothing is served from a change tracker.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <returns>The stored messages.</returns>
    private static async Task<List<OutboxMessage>> GetMessagesAsync(TestedApp<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return await context.Set<OutboxMessage>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Builds an application with a fixed clock and the given outbox options.
    /// </summary>
    /// <param name="configureOptions">Outbox options to override.</param>
    /// <returns>The application under test.</returns>
    private TestedApp<Program> Build(Action<OutboxOptions<TestDbContext>> configureOptions)
        => new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(_time)
            .WithServices(services => services.Configure(configureOptions))
            .Build();
}
