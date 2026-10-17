using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        outbox.AddOnSave(new TestOutboxMessage("hello"));

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
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        context.SomeEntities.Add(new SomeAggregateRoot());
        outbox.AddOnSave(new TestOutboxMessage("hello"));
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
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        await using (var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            outbox.AddOnSave(new TestOutboxMessage("hello"));
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
            .WithServices(services => services.Configure<OutboxOptions<TestDbContext>>(options => options.MaxPayloadLength = 16))
            .Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        var exception = Assert.Throws<OutboxException>(
            () => outbox.AddOnSave(new TestOutboxMessage(new string('a', 500))));

        // Assert
        Assert.Contains("16", exception.Message, StringComparison.Ordinal);
        Assert.Contains(TestOutboxMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_a_message_type_the_outbox_does_not_know_throws_naming_it_and_the_context()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        // Rejected right away rather than stored and dead-lettered later
        var exception = Assert.Throws<OutboxException>(() => outbox.AddOnSave(new UnregisteredMessage()));

        // Assert
        Assert.Contains(UnregisteredMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_a_message_that_cannot_be_serialized_throws()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<SerializationDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.AddOutbox<SerializationDbContext>(config => config.WithMessage<UnserializableMessage, UnserializableMessageHandler>());
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<SerializationDbContext>>();

        // Act
        var exception = Assert.Throws<OutboxException>(() => outbox.AddOnSave(new UnserializableMessage(typeof(string))));

        // Assert
        Assert.Contains(UnserializableMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    [Fact]
    public void Adding_a_message_with_a_reference_cycle_throws_an_outbox_exception()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<SerializationDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.AddOutbox<SerializationDbContext>(config => config.WithMessage<CyclicMessage, CyclicMessageHandler>());
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<SerializationDbContext>>();

        var items = new List<object>();
        items.Add(items);

        // Act
        // Documented as an OutboxException. Today System.Text.Json's JsonException escapes unwrapped,
        // because only NotSupportedException is caught.
        var exception = Assert.Throws<OutboxException>(() => outbox.AddOnSave(new CyclicMessage(items)));

        // Assert
        Assert.Contains(CyclicMessage.MessageType.Key, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adding_a_message_with_settings_on_save_stores_them_with_the_callers_commit()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        outbox.AddOnSave(new TestOutboxMessage("later"), new OutboxMessageSettings { AvailableAt = s_initialTime.AddHours(1) });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var message = await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(s_initialTime.UtcDateTime, message.OccurredAt);
        Assert.Equal(s_initialTime.AddHours(1).UtcDateTime, message.AvailableAt);
    }

    [Fact]
    public async Task Adding_a_message_now_without_settings_commits_it_available_at_once()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TestDbContext>>();

        // Act
        var ticket = await outbox.AddNowAsync(new TestOutboxMessage("now"), TestContext.Current.CancellationToken);

        // Assert
        // Read in a scope of its own: committed without the caller ever saving
        using var verificationScope = app.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var message = await verification.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ticket, message.ToTicket());
        Assert.Equal(s_initialTime.UtcDateTime, message.AvailableAt);
    }

    private sealed record UnregisteredMessage : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "test.unregistered.v1";
    }

    private sealed record UnserializableMessage(Type Type) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "test.unserializable.v1";
    }

    private sealed class UnserializableMessageHandler : IOutboxMessageHandler<UnserializableMessage>
    {
        public Task HandleAsync(UnserializableMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record CyclicMessage(List<object> Items) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "test.cyclic.v1";
    }

    private sealed class CyclicMessageHandler : IOutboxMessageHandler<CyclicMessage>
    {
        public Task HandleAsync(CyclicMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SerializationDbContext(DbContextOptions<SerializationDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
        }
    }
}
