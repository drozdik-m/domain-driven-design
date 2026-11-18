using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies scheduled messages: delivered later, and taken back by the transaction
/// whose commit makes them unnecessary - unless a processor got to them first.
/// </summary>
public class OutboxScheduleTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Removing_a_scheduled_message_on_save_removes_it_with_the_callers_commit()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await ScheduleAsync(app, DateTimeOffset.MaxValue);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);
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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await ScheduleAsync(app, s_initialTime);
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);

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
        var ticket = await ScheduleAsync(app, s_initialTime);
        await outbox.RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

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
        var ticket = outbox.AddOnSave(context, new TestOutboxMessage("never"));

        // Act
        await outbox.RemoveOnSaveAsync(context, ticket, TestContext.Current.CancellationToken);
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
        outbox.AddOnSave(context, new TestOutboxMessage("later"), new() { AvailableAt = s_initialTime.AddHours(1) });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var early = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        time.Advance(TimeSpan.FromHours(1));
        var onTime = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

        // Assert
        Assert.Equal(0, early);
        Assert.Equal(1, onTime);
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
        var ticket = await ScheduleAsync(app, s_initialTime.AddHours(1));

        // Act
        var postponed = await outbox.PostponeOnSaveAsync(context, ticket, s_initialTime.AddHours(3), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(2));
        var early = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;
        time.Advance(TimeSpan.FromHours(1));
        var onTime = (await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken)).Dispatched;

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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await ScheduleAsync(app, s_initialTime);
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var postponed = await outbox.PostponeOnSaveAsync(context, ticket, s_initialTime.AddHours(1), TestContext.Current.CancellationToken);

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
        var ticket = await ScheduleAsync(app, s_initialTime);
        await outbox.PostponeOnSaveAsync(context, ticket, s_initialTime, TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Act
        var commit = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(commit);
    }

    [Fact]
    public async Task Postponing_a_message_twice_on_save_commits_the_last_postponement()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await ScheduleAsync(app, s_initialTime.AddHours(1));

        // Act
        // The second postponement goes by the ticket the first one issued, before anything is saved
        var first = await outbox.PostponeOnSaveAsync(context, ticket, s_initialTime.AddHours(2), TestContext.Current.CancellationToken);
        var second = await outbox.PostponeOnSaveAsync(context, first!.Value, s_initialTime.AddHours(3), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // The save still checks the stamp the row had when it was loaded, and stores the last one issued
        Assert.NotNull(second);
        var stored = await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(s_initialTime.AddHours(3).UtcDateTime, stored.AvailableAt);
        Assert.Equal(second.Value, stored.ToTicket());
    }

    [Fact]
    public async Task Removing_a_message_postponed_in_the_same_save_removes_it()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();
        var ticket = await ScheduleAsync(app, DateTimeOffset.MaxValue.AddDays(-1));
        var postponed = await outbox.PostponeOnSaveAsync(context, ticket, DateTimeOffset.MaxValue, TestContext.Current.CancellationToken);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(context, postponed!.Value, TestContext.Current.CancellationToken);
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
        var ticket = await ScheduleAsync(app, DateTimeOffset.MaxValue.AddDays(-1));
        await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        var postponed = await PostponeElsewhereAsync(app, ticket, DateTimeOffset.MaxValue);

        // Act
        var removed = await outbox.RemoveOnSaveAsync(context, postponed!.Value, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, await CountAsync(context));
    }

    /// <summary>
    /// Commits a message for later in a scope of its own, so the context of the test does not track it.
    /// </summary>
    /// <param name="app">The app to commit through.</param>
    /// <param name="availableAt">When the message becomes due.</param>
    /// <returns>The ticket of the message.</returns>
    private static async Task<OutboxTicket> ScheduleAsync(TestedApp<Program> app, DateTimeOffset availableAt)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var ticket = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>()
            .AddOnSave(context, new TestOutboxMessage("later"), new() { AvailableAt = availableAt });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ticket;
    }

    /// <summary>
    /// Postpones a message and commits it in a scope of its own, behind the back of the context of the test.
    /// </summary>
    /// <param name="app">The app to commit through.</param>
    /// <param name="ticket">The ticket of the message.</param>
    /// <param name="availableAt">The new time the message becomes due.</param>
    /// <returns>The new ticket, or null when nothing was postponed.</returns>
    private static async Task<OutboxTicket?> PostponeElsewhereAsync(TestedApp<Program> app, OutboxTicket ticket, DateTimeOffset availableAt)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var postponed = await scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>()
            .PostponeOnSaveAsync(context, ticket, availableAt, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return postponed;
    }

    /// <summary>
    /// Counts the stored messages, bypassing anything tracked.
    /// </summary>
    /// <param name="context">The context to count through.</param>
    /// <returns>The number of messages.</returns>
    private static Task<int> CountAsync(TestDbContext context)
        => context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
}
