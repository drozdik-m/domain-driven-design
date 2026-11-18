using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Guards added by the outbox review.
/// </summary>
public sealed class OutboxReviewTests : OutboxInstancesTestBase
{
    [Fact]
    public async Task A_stored_row_with_a_blank_payload_does_not_block_the_outbox_without_detailed_errors()
    {
        // Arrange
        // Between two healthy messages, as in OutboxProcessorTests, but without detailed errors
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(instance, [new ReviewMessage("before")]);
        Time.Advance(TimeSpan.FromSeconds(1));
        var unreadableId = await AddRawRowAsync(instance, ReviewMessage.MessageType.Key, " ", Time.GetUtcNow().UtcDateTime);
        Time.Advance(TimeSpan.FromSeconds(1));
        await EnqueueAsync(instance, [new ReviewMessage("after")]);

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Dispatched);
        Assert.Equal(["before", "after"], Hooks.Handled);

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
    public async Task A_context_that_does_not_track_queries_by_default_records_the_deliveries_of_a_batch_read_row_by_row()
    {
        // Arrange
        // The unreadable row sends the batch down the row by row fallback, which must track what it reads as well
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            configureContext: static options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        await EnqueueAsync(instance, [new ReviewMessage("before")]);
        Time.Advance(TimeSpan.FromSeconds(1));
        var unreadableId = await AddRawRowAsync(instance, ReviewMessage.MessageType.Key, " ", Time.GetUtcNow().UtcDateTime);
        Time.Advance(TimeSpan.FromSeconds(1));
        await EnqueueAsync(instance, [new ReviewMessage("after")]);

        // Act
        var first = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var second = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, first.Dispatched);
        Assert.Equal(0, second.Dispatched);
        Assert.Equal(["before", "after"], Hooks.Handled);

        await using var scope = instance.Services.CreateAsyncScope();
        var delivered = await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .Where(m => m.Id != unreadableId)
            .Select(m => new { m.ProcessedAt, m.Attempts })
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, delivered.Count);
        Assert.All(delivered, message =>
        {
            Assert.NotNull(message.ProcessedAt);
            Assert.Equal(1, message.Attempts);
        });
    }

    [Fact]
    public async Task Enqueuing_a_type_that_only_shares_the_key_of_a_registered_type_throws()
    {
        // Arrange
        // ImpostorMessage is registered nowhere, it only reuses the key of ReviewMessage
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();

        // Act
        var exception = Record.Exception(() => outbox.AddOnSave(context, new ImpostorMessage(42)));

        // Assert
        Assert.IsType<OutboxException>(exception);
    }

    [Fact]
    public async Task A_type_sharing_the_key_of_a_registered_type_is_never_handed_to_the_handler_of_that_type()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        try
        {
            await EnqueueAsync(instance, [new ImpostorMessage(42)]);
        }
        catch (OutboxException)
        {
            // Rejected at enqueue time, which is the fix
        }

        // Act
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // {"Number":42} read as a ReviewMessage is a ReviewMessage with a null Text
        Assert.Empty(Hooks.Handled);
    }

    [Theory]
    [InlineData("""{"Number":42}""")]
    [InlineData("""{"Text":null}""")]
    [InlineData("""{"Text":"a","Extra":1}""")]
    public async Task A_stored_payload_that_no_longer_fits_its_type_is_dead_lettered_instead_of_delivered(string payload)
    {
        // Arrange
        // E.g. a v1 row read by a type that gained a required member without a key bump
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options => options.RetryDelays = []);
        await AddRawRowAsync(instance, ReviewMessage.MessageType.Key, payload, InitialTime.UtcDateTime);

        // Act
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(Hooks.Handled);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.Null(message.ProcessedAt);
        Assert.NotNull(message.FailedAt);
    }

    [Fact]
    public async Task Enqueuing_a_message_with_null_in_a_non_nullable_member_throws_an_outbox_exception()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();

        // Act
        var exception = Record.Exception(() => outbox.AddOnSave(context, new ReviewMessage(null!)));

        // Assert
        Assert.IsType<OutboxException>(exception);
    }

    [Fact]
    public async Task A_context_that_does_not_track_queries_by_default_still_records_every_delivery()
    {
        // Arrange
        // A common setting of read-heavy applications, applied to the context the processor borrows
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            configureContext: static options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        await EnqueueAsync(instance, [new ReviewMessage("once")]);

        // Act
        var first = await RecordAsync(() => instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken));
        var second = await RecordAsync(() => instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(["once"], Hooks.Handled);

        await using var scope = instance.Services.CreateAsyncScope();
        var message = await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_context_that_does_not_track_queries_by_default_keeps_a_message_in_delivery_from_other_instances()
    {
        // Arrange
        // The delivery takes two minutes: past the one minute reservation, well within the five minute lease
        static void NoTracking(DbContextOptionsBuilder options) => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), configureContext: NoTracking);
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), configureContext: NoTracking);
        await EnqueueAsync(first, [new ReviewMessage("slow")]);

        OutboxBatchResult? secondResult = null;
        var secondHasPolled = false;
        Hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (secondHasPolled)
            {
                return;
            }

            secondHasPolled = true;
            Time.Advance(TimeSpan.FromMinutes(2));
            secondResult = await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
        };

        // Act
        await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The claim holds the message for the lease, so the second instance must find nothing to deliver
        Assert.NotNull(secondResult);
        Assert.Equal(0, secondResult.Dispatched);
        Assert.Equal(["slow"], Hooks.Handled);
    }

    [Fact]
    public async Task Removing_a_message_on_save_works_on_a_context_that_does_not_track_queries_by_default()
    {
        // Arrange
        // Blob storage takes back the grace removal of every upload this way
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            configureContext: static options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        var ticket = await EnqueueWithTicketAsync(instance, new ReviewMessage("taken back"));

        // Act
        bool removed;
        await using (var scope = instance.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
            removed = await scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>().RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.True(removed);
        Assert.Empty(await GetMessagesAsync(instance));
    }

    [Fact]
    public async Task Postponing_a_message_on_save_works_on_a_context_that_does_not_track_queries_by_default()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            configureContext: static options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        var ticket = await EnqueueWithTicketAsync(instance, new ReviewMessage("later"));
        var later = InitialTime.AddHours(1);

        // Act
        OutboxTicket? postponed;
        await using (var scope = instance.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
            postponed = await scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>().PostponeOnSaveAsync(context, ticket, later, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.NotNull(postponed);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.Equal(later.UtcDateTime, message.AvailableAt);
        Assert.Equal(postponed.Value, message.ToTicket());
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
        Assert.Equal(["once"], Hooks.Handled);

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
        Hooks.OnHandleAsync = async (_, _) =>
        {
            if (Hooks.Handled.Count >= 5)
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
        Assert.Equal(["once"], Hooks.Handled);
    }

    [Fact]
    public async Task A_context_that_does_not_detect_changes_automatically_records_a_failed_delivery_as_a_retry()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), detectChangesAutomatically: false);
        await EnqueueAsync(instance, [new ReviewMessage("failing")]);
        Hooks.OnHandleAsync = static (_, _) => throw new InvalidOperationException("Boom");

        // Act
        var first = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var second = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, first.Dispatched);
        Assert.Equal(0, second.Found);
        Assert.Equal(["failing"], Hooks.Handled);

        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("Boom", message.LastError, StringComparison.Ordinal);
        Assert.True(message.AvailableAt > Time.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task Removing_a_message_on_save_reports_false_once_it_was_delivered_after_the_caller_saved_it()
    {
        // Arrange
        // The caller saved the message and still tracks it when a processor delivers it
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
        var ticket = outbox.AddOnSave(context, new ReviewMessage("delivered"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);
        var save = await RecordAsync(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        // The ticket is stale since the claim, so the removal is refused up front instead of failing the caller's save
        Assert.False(removed);
        Assert.Null(save);
        Assert.NotNull(Assert.Single(await GetMessagesAsync(instance)).ProcessedAt);
    }

    [Fact]
    public async Task Postponing_a_message_on_save_reports_null_once_it_was_delivered_after_the_caller_saved_it()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
        var ticket = outbox.AddOnSave(context, new ReviewMessage("delivered"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeOnSaveAsync(context, ticket, InitialTime.AddHours(1), TestContext.Current.CancellationToken);
        var save = await RecordAsync(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(postponed);
        Assert.Null(save);
    }

    [Fact]
    public async Task Postponing_a_dead_letter_by_its_own_ticket_on_save_reports_null_and_leaves_it_as_it_was()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options => options.RetryDelays = []);
        await EnqueueAsync(instance, [new ReviewMessage("poison")]);
        Hooks.OnHandleAsync = static (_, _) => throw new InvalidOperationException("Boom");
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var deadLetter = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(deadLetter.FailedAt);

        // Act
        OutboxTicket? postponed;
        await using (var scope = instance.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
            postponed = await scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>()
                .PostponeOnSaveAsync(context, deadLetter.ToTicket(), InitialTime.AddHours(1), TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.Null(postponed);
        var stored = Assert.Single(await GetMessagesAsync(instance));
        Assert.Equal(deadLetter.AvailableAt, stored.AvailableAt);
        Assert.Equal(deadLetter.ConcurrencyStamp, stored.ConcurrencyStamp);
    }

    [Fact]
    public async Task A_dead_letter_redriven_as_documented_is_delivered_again()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options => options.RetryDelays = []);
        await EnqueueAsync(instance, [new ReviewMessage("poison")]);
        var failing = true;
        Hooks.OnHandleAsync = (_, _) => failing ? throw new InvalidOperationException("Boom") : Task.CompletedTask;
        await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        Assert.NotNull(Assert.Single(await GetMessagesAsync(instance)).FailedAt);

        // Act
        // The cause is fixed, then the row is re-driven as OutboxDiagram.md describes
        failing = false;
        await using (var scope = instance.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
                .Set<OutboxMessage>()
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(m => m.FailedAt, (DateTime?)null)
                        .SetProperty(m => m.LastError, (string?)null)
                        .SetProperty(m => m.Attempts, 0)
                        .SetProperty(m => m.ConcurrencyStamp, Guid.CreateVersion7()),
                    TestContext.Current.CancellationToken);
        }

        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Dispatched);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task Enqueuing_a_message_whose_members_collide_in_json_throws_an_outbox_exception()
    {
        // Arrange
        // Url and URL are both "url" under the camel case naming of the web serializer options
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<CollidingMessage, CollidingMessageHandler>(),
            static options => options.SerializerOptions = System.Text.Json.JsonSerializerOptions.Web);
        await using var scope = instance.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();

        // Act
        var exception = Record.Exception(() => outbox.AddOnSave(context, new CollidingMessage("https://a.example", "https://b.example")));

        // Assert
        Assert.IsType<OutboxException>(exception);
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

    /// <summary>
    /// A different message that happens to declare the key of <see cref="OutboxInstancesTestBase.ReviewMessage"/>, e.g. copied from it or from another context.
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

    private sealed class CollidingMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<CollidingMessage>
    {
        public Task HandleAsync(CollidingMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue(message.Url);
            return Task.CompletedTask;
        }
    }
}
