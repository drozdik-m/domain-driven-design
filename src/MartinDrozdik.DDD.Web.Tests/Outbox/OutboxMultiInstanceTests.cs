using System.Collections.Concurrent;
using System.Data.Common;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly TestLogger _logger = new();
    private readonly List<IHost> _instances = [];

    [Fact]
    public async Task Two_instances_processing_at_the_same_time_deliver_every_message_exactly_once()
    {
        // Arrange
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(first, Enumerable.Range(0, 30).Select(i => new ReviewMessage($"message-{i}")));

        // Both poll at once and race to reserve the same rows
        _hooks.OnHandleAsync = static async (_, _) => await Task.Yield();

        // Act
        var results = await Task.WhenAll(
            Task.Run(() => first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken),
            Task.Run(() => second.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(30, results.Sum(r => r.Dispatched));
        Assert.Equal(30, results.Sum(r => r.Reserved));
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
            secondDispatched = (await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken)).Dispatched;
        };

        // Act
        var firstDispatched = (await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        // The first instance reserved its ten up front, so the second reads past them and takes the other ten
        Assert.Equal(10, firstDispatched);
        Assert.Equal(10, secondDispatched);
        Assert.Equal(20, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task An_instance_does_not_reserve_rows_another_instance_holds()
    {
        // Arrange
        static void Options(OutboxOptions<ReviewDbContext> options) => options.BatchSize = 3;
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(first, Enumerable.Range(0, 4).Select(i => new ReviewMessage($"message-{i}")));

        // While the first instance delivers its first message, look at the rows and let the second instance poll
        List<OutboxMessage> duringFirstBatch = [];
        OutboxBatchResult? secondResult = null;
        var secondHasPolled = false;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (secondHasPolled)
            {
                return;
            }

            secondHasPolled = true;
            duringFirstBatch = await GetMessagesAsync(first);
            secondResult = await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
        };

        // Act
        var firstResult = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Three rows held by the first run - one claimed, two reserved - and one left free
        var held = duringFirstBatch.Where(m => m.ClaimedBy is not null).ToList();
        Assert.Equal(3, held.Count);
        Assert.Single(held.Select(m => m.ClaimedBy).Distinct());
        Assert.Equal(2, held.Count(m => m.ReservedUntil is not null));
        Assert.Single(duringFirstBatch, m => m.ClaimedBy is null);

        // So the second instance only got the free one
        Assert.NotNull(secondResult);
        Assert.Equal(1, secondResult.Reserved);
        Assert.Equal(1, secondResult.Dispatched);
        Assert.Equal(3, firstResult.Dispatched);
        Assert.Equal(4, _hooks.Handled.Count);
        Assert.Equal(4, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task A_slow_message_lets_the_rest_of_its_batch_expire_to_another_instance()
    {
        // Arrange
        static void Options(OutboxOptions<ReviewDbContext> options)
        {
            options.BatchSize = 3;
            options.ReservationDuration = TimeSpan.FromMinutes(1);
            options.LeaseDuration = TimeSpan.FromMinutes(10);
        }

        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(first, Enumerable.Range(0, 3).Select(i => new ReviewMessage($"message-{i}")));

        // The first message outlasts the reservation of the batch, and the second instance polls meanwhile
        OutboxBatchResult? secondResult = null;
        var secondHasPolled = false;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (secondHasPolled)
            {
                return;
            }

            secondHasPolled = true;
            _time.Advance(TimeSpan.FromMinutes(2));
            secondResult = await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
        };

        // Act
        var firstResult = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The slow message is still leased by the first instance, the other two went to the second
        Assert.NotNull(secondResult);
        Assert.Equal(2, secondResult.Reserved);
        Assert.Equal(2, secondResult.Dispatched);
        Assert.Equal(3, firstResult.Reserved);
        Assert.Equal(1, firstResult.Dispatched);

        // Each message delivered once, none attempted twice
        Assert.Equal(3, _hooks.Handled.Count);
        Assert.Equal(3, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());
        var messages = await GetMessagesAsync(first);
        Assert.All(messages, m => Assert.NotNull(m.ProcessedAt));
        Assert.All(messages, m => Assert.Equal(1, m.Attempts));
        Assert.All(messages, m => Assert.Null(m.ReservedUntil));
    }

    [Fact]
    public async Task An_instance_skips_a_message_whose_reservation_expired_even_when_nobody_took_it()
    {
        // Arrange
        // The reservation is a promise to other processors: once it is over, the message is theirs, taken or not
        static void Options(OutboxOptions<ReviewDbContext> options)
        {
            options.BatchSize = 2;
            options.ReservationDuration = TimeSpan.FromMinutes(1);
        }

        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(instance, [new ReviewMessage("first"), new ReviewMessage("second")]);

        // Whichever message is delivered first outlasts the reservation
        var slowHandled = false;
        _hooks.OnHandleAsync = (_, _) =>
        {
            if (!slowHandled)
            {
                slowHandled = true;
                _time.Advance(TimeSpan.FromMinutes(2));
            }

            return Task.CompletedTask;
        };

        // Act
        var firstRun = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        var skipped = Assert.Single(await GetMessagesAsync(instance), m => m.ProcessedAt is null);
        var secondRun = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Skipped without an attempt, and released for the next run
        Assert.Equal(2, firstRun.Reserved);
        Assert.Equal(1, firstRun.Dispatched);
        Assert.Equal(0, skipped.Attempts);
        Assert.Null(skipped.ClaimedBy);
        Assert.Null(skipped.ReservedUntil);
        Assert.Null(skipped.ClaimedUntil);

        Assert.Equal(1, secondRun.Dispatched);
        Assert.Equal(2, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, _hooks.Handled.Count);
    }

    [Fact]
    public async Task An_instance_with_a_lagging_clock_cannot_claim_a_message_another_instance_already_claimed()
    {
        // Arrange
        // The second instance runs two minutes ahead, so it sees the reservation of the first instance as expired while the first does not.
        // A reservation does not change the concurrency stamp, so both read the same stamp: only the claim decides who delivers.
        static void Options(OutboxOptions<ReviewDbContext> options)
        {
            options.BatchSize = 2;
            options.ReservationDuration = TimeSpan.FromMinutes(1);
        }

        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            Options,
            new FakeTimeProvider(s_initialTime.AddMinutes(2)));
        await EnqueueAsync(first, [new ReviewMessage("first"), new ReviewMessage("second")]);

        // While the first instance delivers its first message, the second takes the other one and delivers it
        string? delivered = null;
        OutboxBatchResult? secondResult = null;
        _hooks.OnHandleAsync = async (text, cancellationToken) =>
        {
            if (delivered is not null)
            {
                return;
            }

            delivered = text;
            secondResult = await second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
        };

        // Act
        var firstResult = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The first instance still believed in its reservation, tried to claim, and lost to the stamp
        Assert.NotNull(secondResult);
        Assert.Equal(1, secondResult.Dispatched);
        Assert.Equal(1, firstResult.Dispatched);
        Assert.Equal(2, _hooks.Handled.Count);
        Assert.Equal(2, _hooks.Handled.Distinct(StringComparer.Ordinal).Count());

        var messages = await GetMessagesAsync(first);
        Assert.All(messages, m => Assert.NotNull(m.ProcessedAt));
        Assert.All(messages, m => Assert.Equal(1, m.Attempts));
    }

    [Fact]
    public async Task A_producer_still_removes_a_message_a_worker_reserved_but_has_not_started()
    {
        // Arrange
        // A reservation keeps the concurrency stamp, so a ticket works until the message is claimed
        var producer = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        var worker = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());

        Dictionary<string, OutboxTicket> tickets = [];
        await using (var scope = producer.Services.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
            tickets["first"] = outbox.AddOnSave(new ReviewMessage("first"));
            tickets["second"] = outbox.AddOnSave(new ReviewMessage("second"));
            await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // While the worker delivers one message, the producer takes back the other one
        bool? removed = null;
        _hooks.OnHandleAsync = async (text, cancellationToken) =>
        {
            if (removed is not null)
            {
                return;
            }

            await using var scope = producer.Services.CreateAsyncScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
            removed = await outbox.RemoveNowAsync(tickets[text == "first" ? "second" : "first"], cancellationToken);
        };

        // Act
        var result = await worker.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The worker had reserved both, lost the claim of the removed one and skipped it
        Assert.True(removed);
        Assert.Equal(2, result.Reserved);
        Assert.Equal(1, result.Dispatched);
        Assert.Single(_hooks.Handled);
        Assert.Single(await GetMessagesAsync(worker));
    }

    [Fact]
    public async Task An_instance_does_not_reserve_messages_another_instance_claimed_after_they_were_read()
    {
        // Arrange
        // Another instance claims every message between this instance reading the ids of its batch and reserving them
        var reserving = new ReserveInterceptor();
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), interceptor: reserving);
        var rival = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(instance, Enumerable.Range(0, 3).Select(i => new ReviewMessage($"message-{i}")));

        var rivalClaimId = Guid.CreateVersion7();
        reserving.BeforeReserveAsync = async cancellationToken =>
        {
            await using var scope = rival.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
                .Set<OutboxMessage>()
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(m => m.ClaimedBy, rivalClaimId)
                        .SetProperty(m => m.ClaimedUntil, s_initialTime.UtcDateTime.AddMinutes(10))
                        .SetProperty(m => m.Attempts, 1)
                        .SetProperty(m => m.ConcurrencyStamp, Guid.CreateVersion7()),
                    cancellationToken);
        };

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Reserving them anyway would read back the rival's stamp, claim them as well and deliver them a second time
        Assert.Equal(1, reserving.Runs);
        Assert.Equal(new OutboxBatchResult(Found: 3, Reserved: 0, Released: 0, Dispatched: 0), result);
        Assert.Empty(_hooks.Handled);

        var messages = await GetMessagesAsync(instance);
        Assert.All(messages, m => Assert.Equal(rivalClaimId, m.ClaimedBy));
        Assert.All(messages, m => Assert.Null(m.ReservedUntil));
        Assert.All(messages, m => Assert.Equal(1, m.Attempts));

        // The rival moved them on, so a drain asks again rather than stop
        Assert.True(result.MayHaveMore);
    }

    [Fact]
    public async Task A_drain_stops_when_a_batch_releases_every_message_it_found_untouched()
    {
        // Arrange
        // Every reservation has expired before its first message starts, so each batch hands back what it found.
        // Asking again would find the same messages forever
        var reserving = new ReserveInterceptor();
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), interceptor: reserving);
        await EnqueueAsync(instance, Enumerable.Range(0, 3).Select(i => new ReviewMessage($"message-{i}")));

        using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        reserving.BeforeReserveAsync = async _ =>
        {
            _time.Advance(TimeSpan.FromMinutes(2));

            // Bounds the test should the drain spin
            if (reserving.Runs > 5)
            {
                await iteration.CancelAsync();
            }
        };

        await using var scope = instance.Services.CreateAsyncScope();
        var task = new OutboxDispatchRecurringTask<ReviewDbContext>(scope.ServiceProvider.GetRequiredService<IOutboxProcessor<ReviewDbContext>>());

        // Act
        await task.RunAsync(iteration.Token);

        // Assert
        Assert.Equal(1, reserving.Runs);
        Assert.Empty(_hooks.Handled);

        var messages = await GetMessagesAsync(instance);
        Assert.All(messages, m => Assert.Null(m.ClaimedBy));
        Assert.All(messages, m => Assert.Null(m.ReservedUntil));
        Assert.All(messages, m => Assert.Equal(0, m.Attempts));
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
        var delivered = (await producer.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        // The old worker never claimed it, so no retry was burnt on a message it can never deliver
        Assert.Equal(1, delivered);
        Assert.Equal(["new:new"], _hooks.Handled);

        var message = Assert.Single(await GetMessagesAsync(producer));
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_delivery_that_outlives_its_lease_is_recorded_even_after_another_processor_retook_the_message()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options =>
            {
                options.LeaseDuration = TimeSpan.FromMinutes(1);

                // A retry left, so the other processor attempts the message instead of dead-lettering it as abandoned
                options.RetryDelays = [TimeSpan.FromMinutes(1)];
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
        // The message was delivered, so it must not end up dead-lettered (or retried and delivered yet again)
        Assert.Equal(2, attempts);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.LastError);

        // Attempts counts the attempts started, not the one that delivered
        Assert.Equal(2, message.Attempts);

        // A lease shorter than the delivery is worth a warning: the message may have been delivered twice
        Assert.Single(_logger.AtLeast(LogLevel.Warning), e => e.Message.Contains("after its lease", StringComparison.Ordinal));

        // The retry the failed attempt scheduled is gone with it
        _time.Advance(TimeSpan.FromMinutes(5));
        var later = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        Assert.Equal(0, later.Found);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task A_delivery_that_outlives_its_lease_clears_the_dead_letter_of_its_abandoned_attempt()
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
                // No retry left, so the next run takes the expired lease for an abandoned attempt and dead-letters the message
                _time.Advance(TimeSpan.FromMinutes(2));
                await instance.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
            }
        };

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The attempt was not abandoned after all, it was only slow
        Assert.Equal(1, attempts);
        Assert.Equal(1, result.Dispatched);

        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task A_delivery_that_outlives_its_lease_rejects_the_failure_of_the_attempt_that_retook_the_message()
    {
        // Arrange
        static void Options(OutboxOptions<ReviewDbContext> options)
        {
            options.LeaseDuration = TimeSpan.FromMinutes(1);
            options.RetryDelays = [TimeSpan.FromMinutes(1)];
        }

        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        await EnqueueAsync(first, [new ReviewMessage("slow")]);

        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OutboxBatchResult>? secondRun = null;
        var attempts = 0;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                // The first delivery is slow, its lease runs out, and the second instance retakes the message
                _time.Advance(TimeSpan.FromMinutes(2));
                secondRun = Task.Run(() => second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken), cancellationToken);
                await secondStarted.Task.WaitAsync(cancellationToken);

                // ...and the first delivery succeeds while the second is still running
                return;
            }

            secondStarted.SetResult();
            await secondReleased.Task.WaitAsync(cancellationToken);
            throw new InvalidOperationException("The second delivery failed.");
        };

        // Act
        var firstResult = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        secondReleased.SetResult();
        var secondResult = await secondRun!;

        // Assert
        // The failure came after the recorded delivery, so it neither schedules a retry nor dead-letters the message
        Assert.Equal(1, firstResult.Dispatched);
        Assert.Equal(0, secondResult.Dispatched);

        var message = Assert.Single(await GetMessagesAsync(first));
        Assert.Equal(s_initialTime.UtcDateTime.AddMinutes(2), message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.LastError);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedUntil);
        Assert.Null(message.ReservedUntil);
    }

    [Fact]
    public async Task A_delivery_that_outlives_its_lease_keeps_the_delivery_another_processor_recorded_first()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options => options.LeaseDuration = TimeSpan.FromMinutes(1));
        await EnqueueAsync(instance, [new ReviewMessage("slow")]);

        var attempts = 0;
        OutboxBatchResult? retakenResult = null;
        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                // Another processor retakes the message and delivers it before the slow first delivery finishes
                _time.Advance(TimeSpan.FromMinutes(2));
                retakenResult = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken);
                _time.Advance(TimeSpan.FromMinutes(1));
            }
        };

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, attempts);
        Assert.Equal(1, retakenResult?.Dispatched);
        Assert.Equal(0, result.Dispatched);

        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.Equal(s_initialTime.UtcDateTime.AddMinutes(2), message.ProcessedAt);
        Assert.DoesNotContain(_logger.AtLeast(LogLevel.Warning), e => e.Message.Contains("after its lease", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_delivery_whose_message_was_deleted_meanwhile_records_nothing()
    {
        // Arrange
        var instance = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>());
        await EnqueueAsync(instance, [new ReviewMessage("deleted")]);

        _hooks.OnHandleAsync = async (_, cancellationToken) =>
        {
            // F.e. cleaned up by hand while it was being delivered
            await using var scope = instance.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
                .Set<OutboxMessage>()
                .ExecuteDeleteAsync(cancellationToken);
        };

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result.Dispatched);
        Assert.Empty(await GetMessagesAsync(instance));
    }

    [Fact]
    public async Task A_delivery_that_outlives_its_lease_is_recorded_even_when_its_run_is_cancelled_meanwhile()
    {
        // Arrange
        var instance = await CreateInstanceAsync(
            static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(),
            static options =>
            {
                options.LeaseDuration = TimeSpan.FromMinutes(1);
                options.RetryDelays = [TimeSpan.FromMinutes(1)];
            });
        await EnqueueAsync(instance, [new ReviewMessage("slow")]);

        using var run = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var attempts = 0;
        _hooks.OnHandleAsync = async (_, _) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                // Another processor retakes the message and fails it, then the slow run is cancelled (shutdown, Timeout)
                _time.Advance(TimeSpan.FromMinutes(2));
                await instance.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
                await run.CancelAsync();
                return;
            }

            throw new InvalidOperationException("The second delivery failed.");
        };

        // Act
        var result = await instance.Services.ProcessOutboxAsync<ReviewDbContext>(run.Token);

        // Assert
        // The handler already returned, so the delivery is recorded rather than lost to the cancellation
        Assert.Equal(1, result.Dispatched);
        var message = Assert.Single(await GetMessagesAsync(instance));
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task A_run_that_reserved_a_message_during_its_slow_delivery_does_not_deliver_it_again()
    {
        // Arrange
        // The first instance only knows the slow message, the second one also an older message it handles first
        static void Options(OutboxOptions<ReviewDbContext> options) => options.LeaseDuration = TimeSpan.FromMinutes(1);
        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(
            static config => config
                .WithMessage<ReviewMessage, ReviewMessageHandler>()
                .WithMessage<NewReviewMessage, NewReviewMessageHandler>(),
            Options);
        await EnqueueAsync(second, [new NewReviewMessage("older")]);
        await EnqueueAsync(first, [new ReviewMessage("slow")]);

        var secondBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OutboxBatchResult>? secondRun = null;
        _hooks.OnHandleAsync = async (text, cancellationToken) =>
        {
            switch (text)
            {
                case "slow":
                    // The lease runs out, and the second instance reserves the slow message behind the older one
                    _time.Advance(TimeSpan.FromMinutes(2));
                    secondRun = Task.Run(() => second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken), cancellationToken);
                    await secondBusy.Task.WaitAsync(cancellationToken);
                    return;

                case "new:older":
                    secondBusy.SetResult();
                    await secondReleased.Task.WaitAsync(cancellationToken);
                    return;
            }
        };

        // Act
        // A reservation leaves the stamp alone, so the slow delivery is recorded as usual - and its new stamp turns the reservation away
        var firstResult = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        secondReleased.SetResult();
        var secondResult = await secondRun!;

        // Assert
        Assert.Equal(1, firstResult.Dispatched);
        Assert.Equal(2, secondResult.Reserved);
        Assert.Equal(1, secondResult.Dispatched);
        Assert.Equal(["slow", "new:older"], _hooks.Handled);

        var slow = Assert.Single(await GetMessagesAsync(first), m => m.MessageType.Key == ReviewMessage.MessageType.Key);
        Assert.NotNull(slow.ProcessedAt);
        Assert.Equal(1, slow.Attempts);
        Assert.Empty(_logger.AtLeast(LogLevel.Warning));

        // The reservation of the second run goes with the delivery, it does not linger on the row
        Assert.Null(slow.ClaimedBy);
        Assert.Null(slow.ReservedUntil);
    }

    [Fact]
    public async Task A_retry_recorded_while_another_run_reserved_the_message_is_due_after_its_own_delay()
    {
        // Arrange
        // The first instance only knows the slow message, the second one also an older message it handles first
        static void Options(OutboxOptions<ReviewDbContext> options)
        {
            options.LeaseDuration = TimeSpan.FromMinutes(1);
            options.ReservationDuration = TimeSpan.FromMinutes(1);
            options.RetryDelays = [TimeSpan.FromSeconds(10)];
        }

        var first = await CreateInstanceAsync(static config => config.WithMessage<ReviewMessage, ReviewMessageHandler>(), Options);
        var second = await CreateInstanceAsync(
            static config => config
                .WithMessage<ReviewMessage, ReviewMessageHandler>()
                .WithMessage<NewReviewMessage, NewReviewMessageHandler>(),
            Options);
        await EnqueueAsync(second, [new NewReviewMessage("older")]);
        await EnqueueAsync(first, [new ReviewMessage("slow")]);

        var secondBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OutboxBatchResult>? secondRun = null;
        var slowAttempts = 0;
        _hooks.OnHandleAsync = async (text, cancellationToken) =>
        {
            switch (text)
            {
                case "slow" when Interlocked.Increment(ref slowAttempts) == 1:
                    // The lease runs out, the second instance reserves the slow message behind the older one, and then the slow delivery fails
                    _time.Advance(TimeSpan.FromMinutes(2));
                    secondRun = Task.Run(() => second.Services.ProcessOutboxAsync<ReviewDbContext>(cancellationToken), cancellationToken);
                    await secondBusy.Task.WaitAsync(cancellationToken);
                    throw new InvalidOperationException("The slow delivery failed.");

                case "new:older":
                    secondBusy.SetResult();
                    await secondReleased.Task.WaitAsync(cancellationToken);
                    return;
            }
        };

        await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);
        secondReleased.SetResult();
        await secondRun!;

        // Act
        // The retry delay has passed, the reservation of the second run has not
        _time.Advance(TimeSpan.FromSeconds(10));
        var retry = await first.Services.ProcessOutboxAsync<ReviewDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, retry.Dispatched);
        Assert.Equal(2, slowAttempts);
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
        Action<OutboxOptions<ReviewDbContext>>? configureOptions = null,
        TimeProvider? time = null,
        IInterceptor? interceptor = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(time ?? _time);
        builder.Services.AddSingleton(_hooks);
        builder.Services.AddSingleton<ILoggerProvider>(_logger);
        builder.Services.AddDbContext<ReviewDbContext>(options =>
        {
            options.UseSqlite($"Data Source={_databasePath}");
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }
        });
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

    /// <summary>
    /// Runs <see cref="BeforeReserveAsync"/> right before the processor executes the update that reserves its batch.
    /// </summary>
    private sealed class ReserveInterceptor : DbCommandInterceptor
    {
        public Func<CancellationToken, Task> BeforeReserveAsync { get; set; } = static _ => Task.CompletedTask;

        public int Runs { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            // Only the reservation sets ReservedUntil to a value and filters by AvailableAt
            if (command.CommandText.Contains("\"ReservedUntil\" = @", StringComparison.Ordinal) &&
                command.CommandText.Contains("\"AvailableAt\" <=", StringComparison.Ordinal))
            {
                Runs++;
                await BeforeReserveAsync(cancellationToken);
            }

            return result;
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
        public async Task HandleAsync(NewReviewMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue($"new:{message.Text}");
            await hooks.OnHandleAsync($"new:{message.Text}", cancellationToken);
        }
    }
}
