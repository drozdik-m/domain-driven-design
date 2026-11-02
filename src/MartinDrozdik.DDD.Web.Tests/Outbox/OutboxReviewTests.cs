using System.Collections.Concurrent;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
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
/// Guards added by the outbox review.
/// </summary>
/// <remarks>
/// Every instance is a plain host over one shared SQLite file, with the context registered the way production registers it:
/// without <see cref="DbContextOptionsBuilder.EnableDetailedErrors(bool)"/>, which the Testing environment of the test application turns on.
/// </remarks>
public sealed class OutboxReviewTests : IDisposable
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_outbox_review.db");
    private readonly FakeTimeProvider _time = new(s_initialTime);
    private readonly ReviewHandlerHooks _hooks = new();
    private readonly List<IHost> _instances = [];

    [Fact]
    public async Task A_stored_row_with_a_blank_payload_does_not_block_the_outbox_without_detailed_errors()
    {
        // Arrange
        // Between two healthy messages, as in OutboxProcessorTests, but without detailed errors
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(instance, [new ReviewMessage("before")]);
        _time.Advance(TimeSpan.FromSeconds(1));
        var unreadableId = await AddRawRowAsync(instance, ReviewMessage.MessageType.Key, " ", _time.GetUtcNow().UtcDateTime);
        _time.Advance(TimeSpan.FromSeconds(1));
        await EnqueueAsync(instance, [new ReviewMessage("after")]);

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Dispatched);
        Assert.Equal(["before", "after"], _hooks.Handled);

        await using var scope = instance.Services.CreateAsyncScope();
        var unreadable = await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .Where(m => m.Id == unreadableId)
            .Select(m => new { m.FailedAt, m.LastError })
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(unreadable.FailedAt);
        Assert.Contains("could not be read", unreadable.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_context_that_does_not_detect_changes_automatically_still_records_every_delivery()
    {
        // Arrange
        // ChangeTracker.AutoDetectChangesEnabled = false, a common performance setting
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), detectChangesAutomatically: false);
        await EnqueueAsync(instance, [new ReviewMessage("once")]);

        // Act
        var first = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var second = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, first.Dispatched);
        Assert.Equal(0, second.Found);
        Assert.Equal(["once"], _hooks.Handled);

        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_drain_over_a_context_that_does_not_detect_changes_automatically_delivers_a_message_once()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), detectChangesAutomatically: false);
        await EnqueueAsync(instance, [new ReviewMessage("once")]);

        // Bounds the test should the drain deliver the message again and again
        using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        _hooks.OnHandleAsync = async (_, _) =>
        {
            if (_hooks.Handled.Count >= 5)
            {
                await iteration.CancelAsync();
            }
        };

        // Act
        await using (var scope = instance.Services.CreateAsyncScope())
        {
            var task = new OutboxDispatchRecurringTask<ReviewDbContext>(scope.ServiceProvider.GetRequiredService<IOutboxProcessor<ReviewDbContext>>());
            await RecordAsync(() => task.RunAsync(iteration.Token));
        }

        // Assert
        Assert.Equal(["once"], _hooks.Handled);
    }

    [Fact]
    public async Task A_context_that_does_not_detect_changes_automatically_records_a_failed_delivery_as_a_retry()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), detectChangesAutomatically: false);
        await EnqueueAsync(instance, [new ReviewMessage("failing")]);
        _hooks.OnHandleAsync = static (_, _) => throw new InvalidOperationException("Boom");

        // Act
        var first = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var second = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, first.Dispatched);
        Assert.Equal(0, second.Found);
        Assert.Equal(["failing"], _hooks.Handled);

        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("Boom", message.LastError, StringComparison.Ordinal);
        Assert.True(message.AvailableAt > _time.GetUtcNow().UtcDateTime);
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

    private static async Task<OutboxTicket> EnqueueWithTicketAsync<TMessage>(IHost instance, TMessage message)
        where TMessage : IOutboxMessage
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var ticket = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>().AddOnSave(message);
        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
        return ticket;
    }

    private static async Task<List<OutboxMessage>> GetMessagesAsync(IHost instance)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> AddRawRowAsync(IHost instance, string messageType, string payload, DateTime availableAtUtc)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var id = Guid.CreateVersion7();

        // SQLite stores a GUID as uppercase text
        var storedId = id.ToString().ToUpperInvariant();
        var stamp = Guid.CreateVersion7().ToString().ToUpperInvariant();
        await context.Database.ExecuteSqlAsync(
            $"""INSERT INTO "OutboxMessage" ("Id", "MessageType", "Payload", "OccurredAt", "AvailableAt", "Attempts", "ConcurrencyStamp") VALUES ({storedId}, {messageType}, {payload}, {availableAtUtc}, {availableAtUtc}, {0}, {stamp})""",
            TestContext.Current.CancellationToken);

        return id;
    }

    private static async Task<Exception?> RecordAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<Exception?> RecordAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task<IHost> CreateInstanceAsync(
        Action<OutboxConfig> configure,
        Action<OutboxOptions<ReviewDbContext>>? configureOptions = null,
        Action<DbContextOptionsBuilder>? configureContext = null,
        bool detectChangesAutomatically = true)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton<TimeProvider>(_time);
        builder.Services.AddSingleton(_hooks);
        builder.Services.AddSingleton(new ReviewContextSettings(detectChangesAutomatically));
        builder.Services.AddDbContext<ReviewDbContext>(options =>
        {
            options.UseSqlite($"Data Source={_databasePath}");
            configureContext?.Invoke(options);
        });
        builder.AddOutbox(configureOptions ?? (static _ => { }), configure);

        var instance = builder.Build();
        _instances.Add(instance);

        await using var scope = instance.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return instance;
    }

    private sealed class ReviewDbContext : DbContext
    {
        public ReviewDbContext(DbContextOptions<ReviewDbContext> options, ReviewContextSettings settings)
            : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = settings.DetectChangesAutomatically;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
        }
    }

    /// <summary>
    /// How the context of an instance tracks its entities, set in its constructor as an application would.
    /// </summary>
    /// <param name="DetectChangesAutomatically">The value of <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled"/>.</param>
    private sealed record ReviewContextSettings(bool DetectChangesAutomatically);

    private sealed class ReviewHandlerHooks
    {
        public ConcurrentQueue<string?> Handled { get; } = [];

        public Func<string?, CancellationToken, Task> OnHandleAsync { get; set; } = static (_, _) => Task.CompletedTask;
    }

    private sealed record ReviewMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.message.v1";
    }

    /// <summary>
    /// A different message that happens to declare the key of <see cref="ReviewMessage"/>, e.g. copied from it or from another context.
    /// </summary>
    private sealed record ImpostorMessage(int Number) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.message.v1";
    }

    /// <summary>
    /// A message with two members that serialize under one JSON name.
    /// </summary>
    private sealed record CollidingMessage(string Url, string URL) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.colliding-message.v1";
    }

    private sealed class ReviewMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<ReviewMessage>
    {
        public async Task HandleAsync(ReviewMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue(message.Text);
            await hooks.OnHandleAsync(message.Text, cancellationToken);
        }
    }

    private sealed class CollidingMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<CollidingMessage>
    {
        public Task HandleAsync(CollidingMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue(message.Url);
            return Task.CompletedTask;
        }
    }
}
