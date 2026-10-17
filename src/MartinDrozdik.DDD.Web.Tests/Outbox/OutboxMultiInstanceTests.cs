using System.Collections.Concurrent;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies several application instances working on one outbox table at the same time.
/// </summary>
/// <remarks>
/// Every instance is a host of its own, with its own container and registry, over one shared SQLite file.
/// The clock and the record of handled messages are shared, so a test can drive and observe all of them.
/// </remarks>
public sealed class OutboxMultiInstanceTests : IDisposable
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_outbox_instances.db");
    private readonly FakeTimeProvider _time = new(s_initialTime);
    private readonly ReviewHandlerHooks _hooks = new();
    private readonly List<IHost> _instances = [];

    [Fact]
    public async Task Two_instances_processing_at_the_same_time_deliver_every_message_exactly_once()
    {
        // Arrange
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(first, Enumerable.Range(0, 30).Select(i => new ReviewMessage($"message-{i}")));

        // Both read the same batch, so they race for every message of it
        _hooks.OnHandleAsync = static async (_, _) => await Task.Yield();

        // Act
        var results = await Task.WhenAll(
            Task.Run(() => first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken),
            Task.Run(() => second.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(30, results.Sum());
        Assert.Equal(30, _hooks.Handled.Count);
        Assert.Equal(30, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());

        var messages = await GetMessagesAsync(first);
        Assert.All(messages, m => Assert.NotNull(m.ProcessedAt));
        Assert.All(messages, m => Assert.Equal(1, m.Attempts));
    }

    [Fact]
    public async Task Two_instances_polling_at_the_same_time_each_deliver_a_batch_of_their_own()
    {
        // Arrange
        // A backlog of two batches and two instances: scaling out should drain it in one round
        static void Options(OutboxOptions<ReviewDbContext> options) => options.BatchSize = 10;
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(first, Enumerable.Range(0, 20).Select(i => new ReviewMessage($"message-{i}")));

        // The second instance polls while the first is still delivering its first message
        var secondDispatched = 0;
        var secondHasPolled = false;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (secondHasPolled)
            {
                return;
            }

            secondHasPolled = true;
            secondDispatched = await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
        };

        // Act
        var firstDispatched = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Today both read the same ten rows, so the second instance mostly takes messages the first already holds in its batch:
        // together they deliver 11, and the remaining 9 wait for the next poll
        Assert.Equal(20, firstDispatched + secondDispatched);
    }

    [Fact]
    public async Task A_worker_that_does_not_know_a_message_type_yet_leaves_it_for_an_instance_that_does()
    {
        // Arrange
        // The producer already ships the new message type. The worker, deployed on its own, does not know it yet.
        static void Options(OutboxOptions<ReviewDbContext> options) => options.RetryDelays = [TimeSpan.FromMinutes(1)];
        var producer = await CreateInstanceAsync(
            static config => config
                .WithMessage<ReviewMessage, ReviewMessageHandler>()
                .WithMessage<NewReviewMessage, NewReviewMessageHandler>(),
            Options);
        var worker = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(producer, [new NewReviewMessage("new")]);

        // Act
        // The old worker keeps polling for as long as its new version takes to roll out
        for (var poll = 0; poll < 5; poll++)
        {
            await worker.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
            _time.Advance(TimeSpan.FromMinutes(1));
        }

        // Then an instance that knows the type polls
        var delivered = await producer.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The old worker never claimed it, so no retry was burnt on a message it can never deliver
        Assert.Equal(1, delivered);
        Assert.Equal(["new:new"], _hooks.Handled);

        var message = Assert.Single(await GetMessagesAsync(producer));
        Assert.Equal(1, message.Attempts);
    }

    // TODO [Fact] Implement lease extension during a long-running delivery
    public async Task A_delivery_that_outlives_its_lease_is_recorded_even_after_another_processor_retook_the_message()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options =>
            {
                options.LeaseDuration = TimeSpan.FromMinutes(1);
                options.RetryDelays = [];
            });
        await EnqueueAsync(instance, [new ReviewMessage("slow")]);

        var attempts = 0;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                // The first delivery is slow, its lease runs out, and another processor retakes the message - and fails it
                _time.Advance(TimeSpan.FromMinutes(2));
                await instance.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);

                // ...but the first delivery then succeeds
                return;
            }

            throw new InvalidOperationException("The second delivery failed.");
        };

        // Act
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The message was delivered, so it must not end up dead-lettered (or retried and delivered yet again).
        // Today the successful outcome is rejected by the concurrency token and silently dropped.
        Assert.Equal(2, attempts);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var instance in _instances)
        {
            instance.Dispose();
        }

        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Left to the OS temp cleanup
        }
    }

    private async Task<IHost> CreateInstanceAsync(
        Action<OutboxConfig> configure,
        Action<OutboxOptions<ReviewDbContext>>? configureOptions = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton<TimeProvider>(_time);
        builder.Services.AddSingleton(_hooks);
        builder.Services.AddDbContext<ReviewDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
        builder.AddOutbox(configureOptions ?? (static _ => { }), configure);

        var instance = builder.Build();
        _instances.Add(instance);

        await using var scope = instance.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return instance;
    }

    private static async Task EnqueueAsync<TMessage>(IHost instance, IEnumerable<TMessage> messages)
        where TMessage : IOutboxMessage
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
        foreach (var message in messages)
        {
            outbox.AddOnSave(message);
        }

        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<OutboxMessage>> GetMessagesAsync(IHost instance)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed class ReviewDbContext(DbContextOptions<ReviewDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
        }
    }

    private sealed class ReviewHandlerHooks
    {
        public ConcurrentQueue<string> Handled { get; } = [];

        public Func<string, CancellationToken, Task> OnHandleAsync { get; set; } = static (_, _) => Task.CompletedTask;
    }

    private sealed record ReviewMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.message.v1";
    }

    private sealed record NewReviewMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.new-message.v1";
    }

    private sealed class ReviewMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<ReviewMessage>
    {
        public async Task HandleAsync(ReviewMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue(message.Text);
            await hooks.OnHandleAsync(message.Text, cancellationToken);
        }
    }

    private sealed class NewReviewMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<NewReviewMessage>
    {
        public Task HandleAsync(NewReviewMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue($"new:{message.Text}");
            return Task.CompletedTask;
        }
    }
}
