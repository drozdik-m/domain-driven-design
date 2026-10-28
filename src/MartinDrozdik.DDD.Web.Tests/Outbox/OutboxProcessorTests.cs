using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies delivery, retries, dead-lettering and claiming against a real database.
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
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var afterFirst = await GetSingleMessageAsync(app);

        // Second attempt fails, so the message waits a minute
        time.Advance(TimeSpan.FromSeconds(10));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var afterSecond = await GetSingleMessageAsync(app);

        // Third attempt has no delay left, so the message is dead-lettered
        time.Advance(TimeSpan.FromMinutes(1));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Act
        time.Advance(TimeSpan.FromMinutes(4));
        var tooEarly = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        time.Advance(TimeSpan.FromMinutes(1));
        var onTime = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // Act
        time.Advance(TimeSpan.FromDays(1));
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_message_of_an_unregistered_type_is_left_untouched_however_long_the_outbox_runs()
    {
        // A rolling deployment, or a separate worker app, lets an instance see a message type only another one registers.
        // Claiming it could only burn its retries and dead-letter it, so it is left for an instance that knows it.

        // Arrange
        using var app = Build(out var time, options => options.RetryDelays = [TimeSpan.FromMinutes(5)], out var logger);
        var stored = OutboxMessage.Create("test.not-registered.v1", "{}", s_initialTime.UtcDateTime);
        await AddRawMessageAsync(app, stored);

        // Act
        // Well past every retry delay
        var dispatched = 0;
        for (var run = 0; run < 3; run++)
        {
            dispatched += (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
            time.Advance(TimeSpan.FromMinutes(10));
        }

        // Assert
        Assert.Equal(0, dispatched);

        var message = await GetSingleMessageAsync(app);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.LastError);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedUntil);
        Assert.Equal(stored.AvailableAt, message.AvailableAt);
        Assert.Equal(stored.ConcurrencyStamp, message.ConcurrencyStamp);

        // Nothing was attempted, so nothing failed
        Assert.DoesNotContain(logger.At(LogLevel.Warning), e => e.Message.StartsWith("Outbox of", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.At(LogLevel.Error), e => e.Message.StartsWith("Outbox of", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Messages_of_an_unregistered_type_waiting_the_longest_do_not_hold_back_a_registered_one()
    {
        // Arrange
        // A whole batch of them at the head of the queue
        using var app = Build(out _, options => options.BatchSize = 2);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        for (var minutes = 3; minutes > 0; minutes--)
        {
            await AddRawMessageAsync(app, OutboxMessage.Create("test.not-registered.v1", "{}", s_initialTime.UtcDateTime.AddMinutes(-minutes)));
        }

        await EnqueueAsync(app, new TestOutboxMessage("behind them"));

        // Act
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["behind them"], state.Handled);
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
        var first = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        var second = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["abandoned"], state.Handled);
    }

    [Fact]
    public async Task A_payload_that_cannot_be_read_is_retried_and_then_dead_lettered()
    {
        // Arrange
        using var app = Build(out var time, options => options.RetryDelays = [TimeSpan.FromMinutes(5)]);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await AddRawMessageAsync(app, OutboxMessage.Create(TestOutboxMessage.MessageType, "not json at all", s_initialTime.UtcDateTime));

        // Act
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var afterFirst = await GetSingleMessageAsync(app);

        time.Advance(TimeSpan.FromMinutes(5));
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var afterSecond = await GetSingleMessageAsync(app);

        // Assert
        Assert.Empty(state.Handled);
        Assert.Null(afterFirst.FailedAt);
        Assert.Contains("could not be deserialized", afterFirst.LastError, StringComparison.Ordinal);
        Assert.NotNull(afterSecond.FailedAt);
        Assert.Equal(2, afterSecond.Attempts);
    }

    [Fact]
    public async Task A_payload_that_reads_as_null_is_a_failed_delivery()
    {
        // Arrange
        using var app = Build(out _, options => options.RetryDelays = []);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await AddRawMessageAsync(app, OutboxMessage.Create(TestOutboxMessage.MessageType, "null", s_initialTime.UtcDateTime));

        // Act
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Empty(state.Handled);

        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Contains("deserialized to null", message.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_message_waiting_the_longest_is_delivered_first()
    {
        // Arrange
        // The message created first, so with the smaller version 7 id, has been available for a shorter time
        using var app = Build(out _, options => options.BatchSize = 1);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await AddRawMessageAsync(app, OutboxMessage.Create(TestOutboxMessage.MessageType, """{"text":"waiting a minute"}""", s_initialTime.UtcDateTime.AddMinutes(-1)));

        // A version 7 id only orders by the millisecond it was created in
        await Task.Delay(TimeSpan.FromMilliseconds(5), TestContext.Current.CancellationToken);
        await AddRawMessageAsync(app, OutboxMessage.Create(TestOutboxMessage.MessageType, """{"text":"waiting an hour"}""", s_initialTime.UtcDateTime.AddHours(-1)));

        // Act
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["waiting an hour"], state.Handled);
    }

    [Fact]
    public async Task A_run_cancelled_after_a_handler_finished_still_records_the_delivery_and_stops()
    {
        // Arrange
        // E.g. the application shutting down while a handler is running
        using var app = Build(out _);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("one"));
        await EnqueueAsync(app, new TestOutboxMessage("two"));

        using var run = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        state.OnHandled = _ => run.Cancel();

        // Act
        var dispatched = (await app.Services.ProcessOutboxAsync<TestDbContext>(run.Token)).Dispatched;

        // Assert
        // The delivered message is recorded, so it is not delivered again once its lease expires
        Assert.Equal(1, dispatched);
        var handled = Assert.Single(state.Handled);

        var messages = await GetMessagesAsync(app);
        var delivered = Assert.Single(messages, m => m.ProcessedAt is not null);
        Assert.Contains(handled, delivered.Payload.Value, StringComparison.Ordinal);
        Assert.Null(delivered.ClaimedBy);

        // The rest of the batch is not even claimed
        var untouched = Assert.Single(messages, m => m.ProcessedAt is null);
        Assert.Equal(0, untouched.Attempts);
        Assert.Null(untouched.ClaimedBy);
    }

    [Fact]
    public async Task An_attempt_cut_short_by_its_run_being_cancelled_still_counts_towards_dead_lettering()
    {
        // Arrange
        // E.g. a handler slower than the Timeout of the dispatch task: the processor cannot tell that from a shutdown,
        // so it records no outcome and leaves the lease - the attempt was counted when it was claimed
        using var app = Build(
            out var time,
            options =>
            {
                options.RetryDelays = [];
                options.LeaseDuration = TimeSpan.FromMinutes(5);
            },
            out var logger);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("too slow"));

        // Act
        for (var run = 0; run < 3; run++)
        {
            using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            state.OnHandled = _ =>
            {
                iteration.Cancel();
                iteration.Token.ThrowIfCancellationRequested();
            };

            try
            {
                await app.Services.ProcessOutboxAsync<TestDbContext>(iteration.Token);
            }
            catch (OperationCanceledException)
            {
                // The run was cut short
            }

            // The abandoned lease expires before the next run
            time.Advance(TimeSpan.FromMinutes(6));
        }

        // Assert
        // With no retries configured the message gets one attempt, not one per run forever
        Assert.Single(state.Handled);
        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.FailedAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("no outcome", message.LastError, StringComparison.Ordinal);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedUntil);
        Assert.Null(message.ReservedUntil);
        Assert.Contains(logger.At(LogLevel.Error), e => e.Message.Contains("dead-lettered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_attempt_cut_short_is_attempted_again_once_its_lease_expires_while_a_retry_is_left()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.RetryDelays = [TimeSpan.FromSeconds(10)];
                options.LeaseDuration = TimeSpan.FromMinutes(5);
            });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("cut short"));

        using (var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken))
        {
            state.OnHandled = _ =>
            {
                iteration.Cancel();
                iteration.Token.ThrowIfCancellationRequested();
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.Services.ProcessOutboxAsync<TestDbContext>(iteration.Token));
        }

        state.OnHandled = static _ => { };

        // Act
        // The lease still holds, then it expires
        var whileLeased = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        time.Advance(TimeSpan.FromMinutes(6));
        var afterLease = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(0, whileLeased);
        Assert.Equal(1, afterLease);
        Assert.Equal(["cut short", "cut short"], state.Handled);

        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public async Task An_attempt_throwing_its_own_exception_after_its_run_was_cancelled_is_cut_short_not_failed()
    {
        // Arrange
        // E.g. SqlClient reporting the cancellation as a SqlException, or a handler wrapping it in an exception of its own
        using var app = Build(
            out _,
            options =>
            {
                options.RetryDelays = [];
                options.LeaseDuration = TimeSpan.FromMinutes(5);
            },
            out var logger);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("interrupted"));

        using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        state.OnHandled = _ =>
        {
            iteration.Cancel();
            throw new HttpRequestException("The connection was aborted.", new TaskCanceledException());
        };

        // Act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.Services.ProcessOutboxAsync<TestDbContext>(iteration.Token));

        // Assert
        Assert.IsType<HttpRequestException>(exception.InnerException);

        // Left like any other attempt cut short: no failure recorded, and the last attempt is not dead-lettered at once
        var message = await GetSingleMessageAsync(app);
        Assert.Null(message.FailedAt);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.LastError);
        Assert.Equal(1, message.Attempts);
        Assert.NotNull(message.ClaimedUntil);

        Assert.Single(logger.At(LogLevel.Warning), e => e.Message.Contains("threw after its run was cancelled", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.At(LogLevel.Error), e => e.Message.StartsWith("Outbox of", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_attempt_throwing_its_own_exception_after_its_run_was_cancelled_is_attempted_again_once_its_lease_expires()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.RetryDelays = [TimeSpan.FromSeconds(10)];
                options.LeaseDuration = TimeSpan.FromMinutes(5);
            });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("interrupted"));

        using (var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken))
        {
            state.OnHandled = _ =>
            {
                iteration.Cancel();
                throw new InvalidOperationException("Sending failed.", new OperationCanceledException(iteration.Token));
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.Services.ProcessOutboxAsync<TestDbContext>(iteration.Token));
        }

        state.OnHandled = static _ => { };

        // Act
        // No retry delay applies, only the lease: still held after the ten seconds of the retry delay, then expired
        time.Advance(TimeSpan.FromSeconds(10));
        var whileLeased = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        time.Advance(TimeSpan.FromMinutes(6));
        var afterLease = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(0, whileLeased);
        Assert.Equal(1, afterLease);
        Assert.Equal(["interrupted", "interrupted"], state.Handled);

        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public async Task Reserving_does_not_count_an_attempt_or_change_the_ticket()
    {
        // Arrange
        using var app = Build(out _, options => options.BatchSize = 3);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));
        await EnqueueAsync(app, new TestOutboxMessage("c"));
        var stampsBefore = (await GetMessagesAsync(app)).ToDictionary(m => m.Id, m => m.ConcurrencyStamp);

        // Look at the rows while the first message of the batch is being delivered
        List<OutboxMessage> duringFirst = [];
        state.OnHandled = _ =>
        {
            if (duringFirst.Count == 0)
            {
                duringFirst = GetMessages(app);
            }
        };

        // Act
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The two waiting messages are only reserved: no attempt, no lease, the same stamp
        var waiting = duringFirst.Where(m => m.ReservedUntil is not null).ToList();
        Assert.Equal(2, waiting.Count);
        Assert.All(waiting, m => Assert.Equal(0, m.Attempts));
        Assert.All(waiting, m => Assert.Null(m.ClaimedUntil));
        Assert.All(waiting, m => Assert.Equal(stampsBefore[m.Id], m.ConcurrencyStamp));

        // The one being delivered is claimed by the same run
        var started = Assert.Single(duringFirst, m => m.ReservedUntil is null);
        Assert.Equal(1, started.Attempts);
        Assert.NotNull(started.ClaimedUntil);
        Assert.NotEqual(stampsBefore[started.Id], started.ConcurrencyStamp);
        Assert.All(waiting, m => Assert.Equal(started.ClaimedBy, m.ClaimedBy));
    }

    [Fact]
    public async Task A_run_cancelled_mid_batch_releases_its_unstarted_reservations()
    {
        // Arrange
        using var app = Build(out _);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));
        await EnqueueAsync(app, new TestOutboxMessage("c"));

        using var iteration = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        state.OnHandled = _ => iteration.Cancel();

        // Act
        var cancelled = await app.Services.ProcessOutboxAsync<TestDbContext>(iteration.Token);
        var released = (await GetMessagesAsync(app)).Where(m => m.ProcessedAt is null).ToList();

        state.OnHandled = static _ => { };
        var next = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, cancelled.Reserved);
        Assert.Equal(1, cancelled.Dispatched);

        // Free again at once and never attempted, so the next run takes them without waiting for any expiry
        Assert.Equal(2, released.Count);
        Assert.All(released, m => Assert.Null(m.ClaimedBy));
        Assert.All(released, m => Assert.Null(m.ReservedUntil));
        Assert.All(released, m => Assert.Equal(0, m.Attempts));

        Assert.Equal(2, next.Dispatched);
        Assert.Equal(3, state.Handled.Count);
    }

    [Fact]
    public async Task A_message_on_its_last_retry_is_not_dead_lettered_when_its_reservation_expired_unstarted()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.RetryDelays = [TimeSpan.FromSeconds(10)];
                options.ReservationDuration = TimeSpan.FromMinutes(1);
            });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();

        // A failed first attempt leaves one retry
        await EnqueueAsync(app, new TestOutboxMessage("retried"));
        state.ShouldFail = true;
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        state.ShouldFail = false;

        // A slow message waiting longer comes first in the next batch and outlasts its reservation
        await EnqueueAsync(app, new TestOutboxMessage("slow"));
        time.Advance(TimeSpan.FromSeconds(10));
        state.OnHandled = text =>
        {
            if (text == "slow")
            {
                time.Advance(TimeSpan.FromMinutes(2));
            }
        };

        // Act
        var skippingRun = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var afterSkip = Assert.Single(await GetMessagesAsync(app), m => m.ProcessedAt is null);
        var laterRun = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Skipping cost nothing: still one attempt, nothing dead-lettered
        Assert.Equal(1, skippingRun.Dispatched);
        Assert.Equal(1, afterSkip.Attempts);
        Assert.Null(afterSkip.FailedAt);
        Assert.Null(afterSkip.ReservedUntil);

        // And the last retry is delivered
        Assert.Equal(1, laterRun.Dispatched);
        var retried = Assert.Single(await GetMessagesAsync(app), m => m.Id == afterSkip.Id);
        Assert.NotNull(retried.ProcessedAt);
        Assert.Equal(2, retried.Attempts);
    }

    [Fact]
    public async Task Every_attempt_gets_a_full_lease_from_its_own_start()
    {
        // Arrange
        using var app = Build(
            out var time,
            options =>
            {
                options.LeaseDuration = TimeSpan.FromMinutes(5);
                options.ReservationDuration = TimeSpan.FromMinutes(1);
            });
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));

        // The first message takes 30 seconds, then look at the lease of the second one while it is delivered
        DateTime? secondLease = null;
        state.OnHandled = _ =>
        {
            if (state.Handled.Count == 1)
            {
                time.Advance(TimeSpan.FromSeconds(30));
                return;
            }

            secondLease = GetMessages(app).Single(m => m.ProcessedAt is null).ClaimedUntil;
        };

        // Act
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // Counted from the start of its own attempt, not from the start of the batch
        Assert.Equal(s_initialTime.UtcDateTime.AddSeconds(30).AddMinutes(5), secondLease);
    }

    [Fact]
    public async Task A_batch_reports_how_many_it_found_reserved_released_and_delivered()
    {
        // Arrange
        using var app = Build(out _, options => options.BatchSize = 2);
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));
        await EnqueueAsync(app, new TestOutboxMessage("c"));

        // Act
        var full = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var partial = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);
        var empty = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // A partial batch may still have more behind it, e.g. messages that became due meanwhile: only an empty one ends a drain
        Assert.Equal(new OutboxBatchResult(Found: 2, Reserved: 2, Released: 0, Dispatched: 2), full);
        Assert.True(full.MayHaveMore);
        Assert.Equal(new OutboxBatchResult(Found: 1, Reserved: 1, Released: 0, Dispatched: 1), partial);
        Assert.True(partial.MayHaveMore);
        Assert.Equal(new OutboxBatchResult(Found: 0, Reserved: 0, Released: 0, Dispatched: 0), empty);
        Assert.False(empty.MayHaveMore);
    }

    [Fact]
    public async Task A_batch_reports_the_messages_it_released_untouched()
    {
        // Arrange
        // The first message outlasts the reservation of the other two
        using var app = Build(out var time, options => options.BatchSize = 3);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("a"));
        await EnqueueAsync(app, new TestOutboxMessage("b"));
        await EnqueueAsync(app, new TestOutboxMessage("c"));
        state.OnHandled = _ => time.Advance(TimeSpan.FromMinutes(2));

        // Act
        var result = await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new OutboxBatchResult(Found: 3, Reserved: 3, Released: 2, Dispatched: 1), result);
        Assert.True(result.MayHaveMore);
    }

    [Fact]
    public async Task A_message_that_failed_more_often_than_the_retry_delays_now_allow_still_gets_its_next_attempt()
    {
        // Only an attempt cut short is dead-lettered without trying again. One whose failure was recorded is waiting
        // for its next attempt, whatever the retry delays were shortened to meanwhile.

        // Arrange
        using var app = Build(out _, options => options.RetryDelays = []);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        var stored = OutboxMessage.Create(TestOutboxMessage.MessageType, """{"text":"retried"}""", s_initialTime.UtcDateTime.AddMinutes(-2));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            stored.Claim(Guid.CreateVersion7(), s_initialTime.UtcDateTime.AddMinutes(-2), TimeSpan.FromMinutes(5));
            stored.MarkRetrying(s_initialTime.UtcDateTime.AddMinutes(-1), TimeSpan.Zero, "Boom");
        }

        await AddRawMessageAsync(app, stored);

        // Act
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["retried"], state.Handled);

        var message = await GetSingleMessageAsync(app);
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(3, message.Attempts);
    }

    [Fact]
    public async Task A_stored_row_with_an_unreadable_message_type_does_not_block_the_rest_of_the_outbox()
    {
        // Arrange
        // A row written by hand, by another writer, or under older key rules - one the current OutboxMessageType rejects
        using var app = Build(out _);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await AddRawRowAsync(app, "legacy key", "{}", s_initialTime.UtcDateTime.AddMinutes(-1));
        await EnqueueAsync(app, new TestOutboxMessage("healthy"));

        // Act
        // Materializing the row would throw an ArgumentException. It is never read, as no registered key matches it
        var dispatched = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(["healthy"], state.Handled);
    }

    [Fact]
    public async Task A_stored_row_with_a_blank_payload_is_dead_lettered_without_blocking_the_rest_of_the_outbox()
    {
        // A registered key matches the query, so the row is read - and materializing its payload throws.
        // So does a spelling the database matches but the key rules reject (uppercase under a case-insensitive collation,
        // trailing spaces on SQL Server), which SQLite cannot reproduce.

        // Arrange
        // Between two healthy messages, so what was read before it is delivered as well as what comes after it
        using var app = Build(out var time, configureOptions: null, out var logger);
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        await EnqueueAsync(app, new TestOutboxMessage("before"));
        time.Advance(TimeSpan.FromSeconds(1));
        var unreadableId = await AddRawRowAsync(app, TestOutboxMessage.MessageType.Key, " ", time.GetUtcNow().UtcDateTime);
        time.Advance(TimeSpan.FromSeconds(1));
        await EnqueueAsync(app, new TestOutboxMessage("after"));

        // Act
        var firstRun = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        var secondRun = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(2, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Equal(["before", "after"], state.Handled);

        // Read without materializing the payload, which still cannot be read
        using var scope = app.Services.CreateScope();
        var unreadable = await scope.ServiceProvider.GetRequiredService<TestDbContext>()
            .Set<OutboxMessage>()
            .Where(m => m.Id == unreadableId)
            .Select(m => new { m.FailedAt, m.ProcessedAt, m.Attempts, m.LastError })
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(time.GetUtcNow().UtcDateTime, unreadable.FailedAt);
        Assert.Null(unreadable.ProcessedAt);
        Assert.Equal(0, unreadable.Attempts);
        Assert.Contains("could not be read", unreadable.LastError, StringComparison.Ordinal);

        // Entity Framework Core logs the failed query on its own
        var error = Assert.Single(logger.At(LogLevel.Error), e => e.Message.StartsWith("Outbox of", StringComparison.Ordinal));
        Assert.Contains(unreadableId.ToString(), error.Message, StringComparison.OrdinalIgnoreCase);
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
    /// Stores a row of raw column values, bypassing every rule of the model, as a hand-written statement or another writer would.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <param name="messageType">The stored message type key.</param>
    /// <param name="payload">The stored payload.</param>
    /// <param name="availableAtUtc">The UTC time the row was enqueued and becomes available.</param>
    /// <returns>The identity of the stored row.</returns>
    private static async Task<Guid> AddRawRowAsync(TestedApp<Program> app, string messageType, string payload, DateTime availableAtUtc)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var id = Guid.CreateVersion7();

        // SQLite stores a GUID as uppercase text
        var storedId = id.ToString().ToUpperInvariant();
        var stamp = Guid.CreateVersion7().ToString().ToUpperInvariant();
        await context.Database.ExecuteSqlAsync(
            $"""INSERT INTO "OutboxMessage" ("Id", "MessageType", "Payload", "OccurredAt", "AvailableAt", "Attempts", "ConcurrencyStamp") VALUES ({storedId}, {messageType}, {payload}, {availableAtUtc}, {availableAtUtc}, {0}, {stamp})""",
            TestContext.Current.CancellationToken);

        return id;
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
    /// Reads every stored message synchronously, for the synchronous handler hook, from a scope of its own.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <returns>The stored messages.</returns>
    private static List<OutboxMessage> GetMessages(TestedApp<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return context.Set<OutboxMessage>().AsNoTracking().ToList();
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

    private TestedApp<Program> Build(out FakeTimeProvider time, Action<OutboxOptions<TestDbContext>>? configureOptions)
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
        Action<OutboxOptions<TestDbContext>>? configureOptions,
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
