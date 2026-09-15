using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies that enqueueing joins the caller's transaction rather than writing on its own.
/// </summary>
public class OutboxEnqueueTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Adding_a_message_writes_no_row_until_the_caller_saves()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        // Act
        outbox.Add(new TestOutboxMessage("hello"));

        // Assert
        var stored = await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, stored);
    }

    [Fact]
    public async Task Saving_writes_the_message_together_with_the_aggregate_that_produced_it()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        // Act
        context.SomeEntities.Add(new SomeAggregateRoot());
        outbox.Add(new TestOutboxMessage("hello"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var message = await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TestOutboxMessage.MessageType, message.MessageType);
        Assert.Contains("hello", message.Payload.Value, StringComparison.Ordinal);
        Assert.Equal(s_initialTime.UtcDateTime, message.OccurredAt);
        Assert.Equal(s_initialTime.UtcDateTime, message.AvailableAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, await context.SomeEntities.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_rolled_back_transaction_leaves_no_message_behind()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        // Act
        await using (var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            outbox.Add(new TestOutboxMessage("hello"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        // A separate scope, so the rolled back row cannot be served from the change tracker
        using var verificationScope = app.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await verification.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, stored);
    }

    [Fact]
    public void Adding_a_message_larger_than_the_configured_limit_throws_naming_both_sizes()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.Configure<OutboxOptions>(options => options.MaxPayloadLength = 16))
            .Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        // Act
        var exception = Assert.Throws<OutboxException>(
            () => outbox.Add(new TestOutboxMessage(new string('a', 500))));

        // Assert
        Assert.Contains("16", exception.Message, StringComparison.Ordinal);
        Assert.Contains(TestOutboxMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
    }
}
