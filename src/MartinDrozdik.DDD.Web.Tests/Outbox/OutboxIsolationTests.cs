using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies that every context has an outbox of its own: its own table, message types, options, processor and trigger.
/// </summary>
/// <remarks>
/// The test app has two contexts, <see cref="TestDbContext"/> and <see cref="SecondTestDbContext"/>, each in a database of its own.
/// Both outboxes register <see cref="TestOutboxMessage"/> with the same handler, and only the second one <see cref="SecondContextOutboxMessage"/>.
/// </remarks>
public class OutboxIsolationTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Each_processor_delivers_only_the_messages_of_its_own_context()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync<TestDbContext, TestOutboxMessage>(app, new TestOutboxMessage("first"));
        await EnqueueAsync<SecondTestDbContext, TestOutboxMessage>(app, new TestOutboxMessage("second"));

        // Act
        var firstDispatched = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var handledByFirst = state.Handled.ToList();
        var secondDispatched = await app.ProcessOutboxAsync<SecondTestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, firstDispatched);
        Assert.Equal(["first"], handledByFirst);
        Assert.Equal(1, secondDispatched);
        Assert.Equal(["first", "second"], state.Handled);
    }

    [Fact]
    public async Task A_message_type_of_another_outbox_is_never_handed_to_its_handler()
    {
        // Arrange
        // The second outbox knows how to deliver this type, but a processor only delivers the types of its own outbox
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(new FakeTimeProvider(s_initialTime))
            .Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await AddRawMessageAsync<TestDbContext>(app, OutboxMessage.Create(SecondContextOutboxMessage.MessageType, """{"orderNumber":1}""", s_initialTime.UtcDateTime));

        // Act
        var dispatched = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Empty(state.Handled);

        // Not even claimed - it is left for an outbox that registers the type
        var message = await GetSingleMessageAsync<TestDbContext>(app);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.ClaimedUntil);
    }

    [Fact]
    public void Enqueuing_a_message_type_of_another_outbox_throws_naming_the_context()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        var exception = Assert.Throws<OutboxException>(() => outbox.AddOnSave(new SecondContextOutboxMessage(1)));

        // Assert
        Assert.Contains(SecondContextOutboxMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_outbox_serializes_with_its_own_options()
    {
        // Arrange
        // The second outbox writes snake_case, the first one the web defaults
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync<SecondTestDbContext, SecondContextOutboxMessage>(app, new SecondContextOutboxMessage(42));

        // Act
        var stored = await GetSingleMessageAsync<SecondTestDbContext>(app);
        var dispatched = await app.ProcessOutboxAsync<SecondTestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("\"order_number\":42", stored.Payload.Value, StringComparison.Ordinal);
        Assert.Equal(1, dispatched);
        Assert.Equal(["second:42"], state.Handled);
    }

    [Fact]
    public async Task Every_outbox_applies_its_own_options()
    {
        // Arrange
        // Only the first outbox forgets delivered messages
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithServices(services => services.Configure<OutboxOptions<TestDbContext>>(options => options.Retention = TimeSpan.FromDays(1)))
            .Build();
        await EnqueueAsync<TestDbContext, TestOutboxMessage>(app, new TestOutboxMessage("first"));
        await EnqueueAsync<SecondTestDbContext, TestOutboxMessage>(app, new TestOutboxMessage("second"));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync<SecondTestDbContext>(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromDays(2));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync<SecondTestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, await CountAsync<TestDbContext>(app));
        Assert.Equal(1, await CountAsync<SecondTestDbContext>(app));
    }

    [Fact]
    public async Task A_commit_wakes_only_the_dispatch_task_of_its_own_context()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var first = Assert.IsType<RecurringTaskTrigger<OutboxDispatchRecurringTask<TestDbContext>>>(
            app.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<TestDbContext>>>());
        var second = Assert.IsType<RecurringTaskTrigger<OutboxDispatchRecurringTask<SecondTestDbContext>>>(
            app.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<SecondTestDbContext>>>());

        // Act
        await EnqueueAsync<SecondTestDbContext, TestOutboxMessage>(app, new TestOutboxMessage("second"));

        // Assert
        await second.WaitAsync(TestContext.Current.CancellationToken);

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await first.WaitAsync(timeout.Token));
    }

    [Fact]
    public async Task An_app_without_the_dispatch_task_only_enqueues_and_a_worker_with_the_identical_outbox_delivers()
    {
        // Arrange
        // Both apps make the identical AddOutbox call. The loops are removed from the producer, as if it never added them,
        // and kept in the worker, which shares the database.
        var producerBuilder = new TestedWebAppBuilder(testOutputHelper);
        using var producer = producerBuilder.Build();
        using var worker = new TestedWebAppBuilder(testOutputHelper)
            .WithOption<DatabaseOptions>(options => options.ConnectionString, producerBuilder.ConnectionString)
            .WithRecurringTasks()
            .Build();

        var producerState = producer.Services.GetRequiredService<TestOutboxHandlerState>();
        var workerState = worker.Services.GetRequiredService<TestOutboxHandlerState>();

        // Act
        await EnqueueAsync<TestDbContext, TestOutboxMessage>(producer, new TestOutboxMessage("from-producer"));

        // The worker's loop is scheduled an hour out, so a delivery can only come from waking it
        worker.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<TestDbContext>>>().Trigger();
        await WaitForHandledAsync(workerState, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["from-producer"], workerState.Handled);
        Assert.Empty(producerState.Handled);

        var message = await GetSingleMessageAsync<TestDbContext>(producer);
        Assert.NotNull(message.ProcessedAt);
    }

    /// <summary>
    /// Enqueues a message into the outbox of a context and commits it, exactly as application code would.
    /// </summary>
    /// <typeparam name="TContext">The context whose outbox the message goes to.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="app">The application under test.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <returns>A <see cref="Task"/> that completes when the message is committed.</returns>
    private static async Task EnqueueAsync<TContext, TMessage>(ITestedApp app, TMessage message)
        where TContext : DbContext
        where TMessage : IOutboxMessage
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TContext>>().AddOnSave(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Stores a message directly in the table of a context, bypassing the registration checks of the outbox.
    /// </summary>
    /// <typeparam name="TContext">The context whose table the message goes to.</typeparam>
    /// <param name="app">The application under test.</param>
    /// <param name="message">The message to store.</param>
    /// <returns>A <see cref="Task"/> that completes when the message is committed.</returns>
    private static async Task AddRawMessageAsync<TContext>(ITestedApp app, OutboxMessage message)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads the only message stored in the table of a context.
    /// </summary>
    /// <typeparam name="TContext">The context whose table is read.</typeparam>
    /// <param name="app">The application under test.</param>
    /// <returns>The stored message.</returns>
    private static async Task<OutboxMessage> GetSingleMessageAsync<TContext>(ITestedApp app)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        return await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Counts the messages stored in the table of a context.
    /// </summary>
    /// <typeparam name="TContext">The context whose table is read.</typeparam>
    /// <param name="app">The application under test.</param>
    /// <returns>The number of stored messages.</returns>
    private static async Task<int> CountAsync<TContext>(ITestedApp app)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        return await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Waits until the outbox handlers have recorded something, or the test is cancelled.
    /// </summary>
    /// <param name="state">The record of handled messages.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>A <see cref="Task"/> that completes once a message has been handled.</returns>
    private static async Task WaitForHandledAsync(TestOutboxHandlerState state, CancellationToken cancellationToken)
    {
        // Bounded, so a delivery that never happens fails the test instead of hanging the suite
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (state.Handled.Count == 0)
        {
            Assert.False(timeout.IsCancellationRequested, "The worker never delivered the message the producer enqueued.");
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
