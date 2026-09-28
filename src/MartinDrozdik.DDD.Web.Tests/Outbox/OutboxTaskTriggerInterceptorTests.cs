using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Interceptors;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies that committing messages wakes the dispatch task instead of leaving them for the next poll.
/// </summary>
public class OutboxTaskTriggerInterceptorTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Committing_a_message_runs_the_dispatch_task_without_waiting_for_the_schedule()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithRecurringTasks()
            .Build();
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();

        // Act
        // The dispatch task is scheduled an hour out, so a delivery can only come from the trigger
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage("prompt"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await WaitForHandledAsync(state, TestContext.Current.CancellationToken);
        Assert.Equal(["prompt"], state.Handled);
    }

    [Fact]
    public async Task A_save_without_messages_does_not_trigger_the_dispatch_task()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var trigger = app.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask>>();
        var concrete = Assert.IsType<RecurringTaskTrigger<OutboxDispatchRecurringTask>>(trigger);

        // Act
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.SomeEntities.Add(new SomeAggregateRoot());
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        // Nothing was queued, so waiting for a trigger times out rather than completing
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await concrete.WaitAsync(timeout.Token));
    }

    [Fact]
    public async Task Scheduling_a_message_for_later_does_not_trigger_the_dispatch_task()
    {
        // Arrange
        // Every upload schedules one - waking the dispatcher for each would find nothing to deliver yet
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var trigger = app.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask>>();
        var concrete = Assert.IsType<RecurringTaskTrigger<OutboxDispatchRecurringTask>>(trigger);

        // Act
        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddNowAsync(
                new TestOutboxMessage("later"),
                new() { AvailableAt = DateTimeOffset.UtcNow.AddHours(1) },
                TestContext.Current.CancellationToken);
        }

        // Assert
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await concrete.WaitAsync(timeout.Token));
    }

    [Fact]
    public async Task Committing_a_message_queues_exactly_one_trigger()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var trigger = app.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask>>();
        var concrete = Assert.IsType<RecurringTaskTrigger<OutboxDispatchRecurringTask>>(trigger);

        // Act
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage("prompt"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        // The first wait is satisfied by the interceptor
        await concrete.WaitAsync(TestContext.Current.CancellationToken);

        // The second finds nothing, because a save with no new messages must not queue another
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await concrete.WaitAsync(timeout.Token));
    }

    [Fact]
    public void Every_context_is_given_an_interceptor_of_its_own()
    {
        // Arrange
        // The interceptor keeps the "this save is writing messages" flag in a field, which is only
        // safe because it is registered scoped and Entity Framework builds the context options once
        // per scope. Sharing one instance would let concurrent saves overwrite each other's flag.
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var firstScope = app.Services.CreateScope();
        using var secondScope = app.Services.CreateScope();

        // Act
        var firstAttached = GetAttachedInterceptors(firstScope.ServiceProvider.GetRequiredService<TestDbContext>());
        var secondAttached = GetAttachedInterceptors(secondScope.ServiceProvider.GetRequiredService<TestDbContext>());

        // Assert
        var first = Assert.Single(firstAttached.OfType<OutboxTaskTriggerInterceptor>());
        var second = Assert.Single(secondAttached.OfType<OutboxTaskTriggerInterceptor>());
        Assert.Same(firstScope.ServiceProvider.GetRequiredService<OutboxTaskTriggerInterceptor>(), first);
        Assert.Same(secondScope.ServiceProvider.GetRequiredService<OutboxTaskTriggerInterceptor>(), second);
        Assert.NotSame(first, second);
    }

    /// <summary>
    /// Waits until the outbox handlers have recorded something, or the test is cancelled.
    /// </summary>
    /// <param name="state">The shared record of handled messages.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>A <see cref="Task"/> that completes once a message has been handled.</returns>
    private static async Task WaitForHandledAsync(TestOutboxHandlerState state, CancellationToken cancellationToken)
    {
        // Bounded, so a trigger that never arrives fails the test instead of hanging the suite
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (state.Handled.Count == 0)
        {
            Assert.False(timeout.IsCancellationRequested, "The outbox message was never delivered after its transaction committed.");
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    /// <summary>
    /// Reads the interceptors Entity Framework actually attached to a context.
    /// </summary>
    /// <param name="context">The context to inspect.</param>
    /// <returns>The attached interceptors.</returns>
    private static IEnumerable<IInterceptor> GetAttachedInterceptors(DbContext context)
    {
        var extension = context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>();
        Assert.NotNull(extension);

        return extension.Interceptors ?? [];
    }
}
