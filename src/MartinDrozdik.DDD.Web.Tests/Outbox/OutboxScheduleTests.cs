using System.Transactions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies scheduled messages: committed on their own, delivered later, and taken back by the transaction
/// whose commit makes them unnecessary - unless a processor got to them first.
/// </summary>
public class OutboxScheduleTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_scheduled_message_survives_a_rollback_of_the_callers_transaction()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Deferred, because SQLite has a single writer and an immediate transaction would hold the write lock the
        // schedule needs - see the remarks of AddOutbox
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        var connection = (SqliteConnection)context.Database.GetDbConnection();

        // Act
        await using (var transaction = connection.BeginTransaction(deferred: true))
        {
            await context.Database.UseTransactionAsync(transaction, TestContext.Current.CancellationToken);
            await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await context.Database.UseTransactionAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, await CountAsync(context));
    }

    [Fact]
    public async Task A_scheduled_message_survives_an_ambient_transaction_that_is_never_completed()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.Equal(1, await CountAsync(context));
    }

    [Fact]
    public async Task A_scheduled_message_is_not_delivered_before_its_time()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);

        // Act
        var early = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(1));
        var onTime = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, early);
        Assert.Equal(1, onTime);
    }

    [Fact]
    public async Task Postponing_a_message_moves_its_delivery_later()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeNowAsync(ticket, s_initialTime.AddHours(3), TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(2));
        var delivered = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(postponed);
        Assert.NotEqual(ticket.ConcurrencyStamp, postponed.Value.ConcurrencyStamp);
        Assert.Equal(0, delivered);
    }

    [Fact]
    public async Task Postponing_a_message_a_processor_has_taken_does_nothing()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeNowAsync(ticket, s_initialTime.AddHours(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(postponed);
    }

    [Fact]
    public async Task Removing_a_scheduled_message_on_save_removes_it_with_the_callers_commit()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = DateTimeOffset.MaxValue }, TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(ticket, TestContext.Current.CancellationToken);
        var beforeSave = await CountAsync(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(removed);
        Assert.Equal(1, beforeSave);
        Assert.Equal(0, await CountAsync(context));
    }

    [Fact]
    public async Task Removing_a_message_a_processor_has_taken_on_save_does_nothing()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(ticket, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public async Task A_commit_removing_a_message_a_processor_has_taken_since_fails()
    {
        // Arrange
        // The message is already being delivered - a transaction that relied on it not being delivered must not commit
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await outbox.RemoveOnSaveAsync(ticket, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var commit = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(commit);
    }

    [Fact]
    public async Task Removing_a_message_added_to_the_same_save_means_it_is_never_written()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = outbox.AddOnSave(new TestOutboxMessage("never"));

        // Act
        await outbox.RemoveOnSaveAsync(ticket, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, await CountAsync(context));
    }

    [Fact]
    public async Task A_message_added_on_save_is_not_delivered_before_its_time()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        outbox.AddOnSave(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime.AddHours(1) });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var early = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(1));
        var onTime = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, early);
        Assert.Equal(1, onTime);
    }

    [Fact]
    public async Task Removing_a_message_now_deletes_it_at_once()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = DateTimeOffset.MaxValue }, TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveNowAsync(ticket, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, await CountAsync(context));
    }

    [Fact]
    public async Task Removing_a_message_a_processor_has_taken_now_does_nothing()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveNowAsync(ticket, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(removed);
        Assert.Equal(1, await CountAsync(context));
    }

    [Fact]
    public async Task Postponing_a_message_on_save_moves_its_delivery_later_with_the_callers_commit()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeOnSaveAsync(ticket, s_initialTime.AddHours(3), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(2));
        var early = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(1));
        var onTime = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(postponed);
        Assert.NotEqual(ticket.ConcurrencyStamp, postponed.Value.ConcurrencyStamp);
        Assert.Equal(0, early);
        Assert.Equal(1, onTime);
    }

    [Fact]
    public async Task Postponing_a_message_a_processor_has_taken_on_save_does_nothing()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeOnSaveAsync(ticket, s_initialTime.AddHours(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(postponed);
    }

    [Fact]
    public async Task A_commit_postponing_a_message_a_processor_has_taken_since_fails()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime }, TestContext.Current.CancellationToken);
        await outbox.PostponeOnSaveAsync(ticket, s_initialTime, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var commit = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(commit);
    }

    [Fact]
    public async Task Removing_a_message_postponed_in_the_same_save_removes_it()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = DateTimeOffset.MaxValue.AddDays(-1) }, TestContext.Current.CancellationToken);
        var postponed = await outbox.PostponeOnSaveAsync(ticket, DateTimeOffset.MaxValue, TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(postponed!.Value, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, await CountAsync(context));
    }

    [Fact]
    public async Task Removing_a_message_on_save_reloads_a_tracked_copy_older_than_the_ticket()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("later"), new() { AvailableAt = DateTimeOffset.MaxValue.AddDays(-1) }, TestContext.Current.CancellationToken);
        await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        var postponed = await outbox.PostponeNowAsync(ticket, DateTimeOffset.MaxValue, TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(postponed!.Value, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, await CountAsync(context));
    }

    /// <summary>
    /// Counts the stored messages, bypassing anything tracked.
    /// </summary>
    /// <param name="context">The context to count through.</param>
    /// <returns>The number of messages.</returns>
    private static Task<int> CountAsync(TestDbContext context)
        => context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
}
