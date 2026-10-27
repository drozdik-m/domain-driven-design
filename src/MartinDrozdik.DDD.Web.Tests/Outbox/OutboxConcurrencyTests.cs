using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies the concurrency token that makes taking a lease safe.
/// </summary>
/// <remarks>
/// Two processors can read the same free message before either writes. The token is what prevents both from claiming it, leaving exactly one winner.
/// </remarks>
public class OutboxConcurrencyTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Two_processors_claiming_the_same_message_leave_exactly_one_winner()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        await EnqueueAsync(app);

        using var firstScope = app.Services.CreateScope();
        using var secondScope = app.Services.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Both read the message while it is still free, as two racing processors would
        var first = await firstContext.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        var second = await secondContext.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);

        var winner = Guid.CreateVersion7();
        var loser = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        // Act
        first.Claim(winner, now, TimeSpan.FromMinutes(5));
        await firstContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        second.Claim(loser, now, TimeSpan.FromMinutes(5));

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        using var verificationScope = app.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await verification.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(winner, stored.ClaimedBy);
    }

    [Fact]
    public async Task A_processor_that_loses_the_race_carries_on_with_the_rest_of_the_batch()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        await EnqueueAsync(app);
        await EnqueueAsync(app);
        await EnqueueAsync(app);

        // While the first message is being delivered, another instance takes one of the reserved ones and bumps its stamp
        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();
        var rivalHasStruck = false;
        state.OnHandled = _ =>
        {
            if (rivalHasStruck)
            {
                return;
            }

            rivalHasStruck = true;
            using var rivalScope = app.Services.CreateScope();
            var rivalContext = rivalScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var contested = rivalContext.Set<OutboxMessage>().First(m => m.ReservedUntil != null);
            contested.MarkProcessed(DateTime.UtcNow);
            rivalContext.SaveChanges();
        };

        // Act
        var result = await app.Services.ProcessOutboxAsync<TestDbContext>(TestContext.Current.CancellationToken);

        // Assert
        // The claim of the contested message failed, but the batch still delivered the other two
        Assert.Equal(3, result.Reserved);
        Assert.Equal(2, result.Dispatched);
        Assert.Equal(2, state.Handled.Count);
    }

    [Fact]
    public async Task A_processor_whose_context_already_tracks_a_message_reads_its_reservation_fresh()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        await EnqueueAsync(app);
        await EnqueueAsync(app);

        var state = app.Services.GetRequiredService<TestOutboxHandlerState>();

        using var processorScope = app.Services.CreateScope();
        var processorContext = processorScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var processor = processorScope.ServiceProvider.GetRequiredService<IOutboxProcessor<TestDbContext>>();

        // The context of the processor tracks both messages before anything is reserved, e.g. left over from earlier work in the scope
        var loaded = await processorContext.Set<OutboxMessage>()
            .OrderBy(m => m.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Another instance delivers the first one meanwhile
        using (var rivalScope = app.Services.CreateScope())
        {
            var rivalContext = rivalScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var contested = await rivalContext.Set<OutboxMessage>()
                .SingleAsync(m => m.Id == loaded[0].Id, TestContext.Current.CancellationToken);
            contested.MarkProcessed(DateTime.UtcNow);
            await rivalContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var result = await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        // The delivered one is not reserved again, and the tracked copy of the other one does not hide its reservation
        Assert.Equal(1, result.Reserved);
        Assert.Equal(1, result.Dispatched);
        Assert.Single(state.Handled);
    }

    private static async Task EnqueueAsync(TestedApp<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>().AddOnSave(new TestOutboxMessage(Guid.NewGuid().ToString()));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
