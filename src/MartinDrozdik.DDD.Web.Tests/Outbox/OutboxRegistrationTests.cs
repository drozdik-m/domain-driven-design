using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies how the outbox registers into a container, without a running application.
/// </summary>
public class OutboxRegistrationTests
{
    [Fact]
    public void Registering_two_message_types_under_one_key_in_one_outbox_throws_naming_both_types()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<RegistrationDbContext>(config => config
                .WithMessage<FirstMessage, FirstMessageHandler>()
                .WithMessage<ClashingMessage, ClashingMessageHandler>()));

        // Assert
        Assert.Contains(nameof(FirstMessage), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ClashingMessage), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Registering_the_same_message_type_twice_with_one_outbox_throws()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<RegistrationDbContext>(config => config
                .WithMessage<FirstMessage, FirstMessageHandler>()
                .WithMessage<FirstMessage, FirstMessageHandler>()));

        // Assert
        Assert.Contains("twice", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(RegistrationDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_the_outbox_twice_over_the_same_context_throws_naming_it()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Act
        // The second registry would be dropped, and its message types with it
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>()));

        // Assert
        Assert.Contains(nameof(RegistrationDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_contexts_each_get_an_outbox_of_their_own()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(
            options => options.BatchSize = 5,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutbox<OtherDbContext>(config => config.WithMessage<SecondMessage, SecondMessageHandler>());
        using var host = builder.Build();

        // Assert
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IOutbox<RegistrationDbContext>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOutbox<OtherDbContext>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOutboxProcessor<RegistrationDbContext>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOutboxProcessor<OtherDbContext>>());

        var first = host.Services.GetRequiredService<OutboxRegistry<RegistrationDbContext>>();
        var second = host.Services.GetRequiredService<OutboxRegistry<OtherDbContext>>();
        Assert.True(first.Contains(FirstMessage.MessageType));
        Assert.False(first.Contains(SecondMessage.MessageType));
        Assert.True(second.Contains(SecondMessage.MessageType));
        Assert.False(second.Contains(FirstMessage.MessageType));

        // Each outbox is configured on its own
        Assert.Equal(5, host.Services.GetRequiredService<IOptions<OutboxOptions<RegistrationDbContext>>>().Value.BatchSize);
        Assert.Equal(100, host.Services.GetRequiredService<IOptions<OutboxOptions<OtherDbContext>>>().Value.BatchSize);
    }

    [Fact]
    public void The_outbox_resolves_only_over_the_context_it_was_added_over()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IOutbox<RegistrationDbContext>>());
        Assert.Null(scope.ServiceProvider.GetService<IOutbox<OtherDbContext>>());
        Assert.Null(scope.ServiceProvider.GetService<IOutboxProcessor<OtherDbContext>>());
    }

    [Fact]
    public void A_message_type_shared_by_two_outboxes_registers_its_handler_once()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // As a shared infrastructure project would, registering its message type with every context's outbox
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutbox<OtherDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Assert
        Assert.Single(builder.Services, d => d.ServiceType == typeof(IOutboxMessageHandler<FirstMessage>));

        using var host = builder.Build();
        Assert.True(host.Services.GetRequiredService<OutboxRegistry<RegistrationDbContext>>().Contains(FirstMessage.MessageType));
        Assert.True(host.Services.GetRequiredService<OutboxRegistry<OtherDbContext>>().Contains(FirstMessage.MessageType));
    }

    [Fact]
    public void Sharing_a_message_type_with_another_handler_throws_naming_both_handlers()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Act
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<OtherDbContext>(config => config.WithMessage<FirstMessage, AnotherFirstMessageHandler>()));

        // Assert
        Assert.Contains(nameof(FirstMessageHandler), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(AnotherFirstMessageHandler), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Another_message_type_under_a_key_used_by_another_outbox_is_mapped_by_each_outbox_to_its_own_type()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // A key identifies one message type within an outbox; every outbox reads its own table through its own registry
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutbox<OtherDbContext>(config => config.WithMessage<ClashingMessage, ClashingMessageHandler>());
        using var host = builder.Build();

        // Assert
        Assert.True(host.Services.GetRequiredService<OutboxRegistry<RegistrationDbContext>>().TryGet(FirstMessage.MessageType, out var first));
        Assert.True(host.Services.GetRequiredService<OutboxRegistry<OtherDbContext>>().TryGet(ClashingMessage.MessageType, out var other));
        Assert.Equal(typeof(FirstMessage), first.MessageClrType);
        Assert.Equal(typeof(ClashingMessage), other.MessageClrType);
    }

    [Fact]
    public void Registering_a_message_type_with_an_uppercase_key_fails_at_startup()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // Fails while the outbox is being added, long before a row carrying the key could exist
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<ShoutingMessage, ShoutingMessageHandler>()));

        // Assert
        Assert.Contains("lowercase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_registry_finds_a_registered_message_type()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();
        var registry = host.Services.GetRequiredService<OutboxRegistry<RegistrationDbContext>>();

        // Act
        var found = registry.TryGet("registration.first.v1", out var registration);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(FirstMessage), registration!.MessageClrType);
        Assert.False(registry.TryGet("registration.unknown.v1", out _));
    }

    [Fact]
    public void The_engine_registers_without_any_background_loop()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        // The processor is there, so another scheduler could drive it
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IOutboxProcessor<RegistrationDbContext>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOutbox<RegistrationDbContext>>());

        // But nothing is polling on its own
        Assert.Empty(host.Services.GetServices<IHostedService>());
    }

    [Fact]
    public void The_dispatch_task_registers_the_recurring_loop()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutboxDispatchRecurringTask<RegistrationDbContext>(options => options.Period = TimeSpan.FromSeconds(30));
        using var host = builder.Build();

        // Assert
        Assert.Single(host.Services.GetServices<IHostedService>().OfType<RecurringTaskHost<OutboxDispatchRecurringTask<RegistrationDbContext>>>());
        var schedule = host.Services.GetRequiredService<IOptions<RecurringTaskOptions<OutboxDispatchRecurringTask<RegistrationDbContext>>>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(30), schedule.Period);
    }

    [Fact]
    public void Every_outbox_gets_a_dispatch_loop_and_a_trigger_of_its_own()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutbox<OtherDbContext>(config => config.WithMessage<SecondMessage, SecondMessageHandler>());

        // Act
        builder.AddOutboxDispatchRecurringTask<RegistrationDbContext>(options => options.Period = TimeSpan.FromSeconds(30));
        builder.AddOutboxDispatchRecurringTask<OtherDbContext>(options => options.Period = TimeSpan.FromMinutes(2));
        using var host = builder.Build();

        // Assert
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        Assert.Single(hostedServices.OfType<RecurringTaskHost<OutboxDispatchRecurringTask<RegistrationDbContext>>>());
        Assert.Single(hostedServices.OfType<RecurringTaskHost<OutboxDispatchRecurringTask<OtherDbContext>>>());

        Assert.Equal(TimeSpan.FromSeconds(30), host.Services.GetRequiredService<IOptions<RecurringTaskOptions<OutboxDispatchRecurringTask<RegistrationDbContext>>>>().Value.Period);
        Assert.Equal(TimeSpan.FromMinutes(2), host.Services.GetRequiredService<IOptions<RecurringTaskOptions<OutboxDispatchRecurringTask<OtherDbContext>>>>().Value.Period);

        var firstTrigger = host.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<RegistrationDbContext>>>();
        var secondTrigger = host.Services.GetRequiredService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<OtherDbContext>>>();
        Assert.NotSame(firstTrigger, secondTrigger);
    }

    [Fact]
    public void The_dispatch_task_cannot_be_added_without_the_outbox_of_its_context()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Act
        // It would resolve no processor and fail on every iteration
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutboxDispatchRecurringTask<OtherDbContext>(options => options.Period = TimeSpan.FromSeconds(30)));

        // Assert
        Assert.Contains(nameof(OtherDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_trigger_is_registered_even_without_the_dispatch_task()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // No dispatch task, as an application driving the processor from Quartz would do
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        // The save interceptor always has something to poke, so triggering is a harmless no-op
        var trigger = host.Services.GetService<IRecurringTaskTrigger<OutboxDispatchRecurringTask<RegistrationDbContext>>>();
        Assert.NotNull(trigger);
        trigger.Trigger();
    }

    [Theory]
    [InlineData(nameof(OutboxOptions<>.BatchSize), "batch size")]
    [InlineData(nameof(OutboxOptions<>.LeaseDuration), "lease duration")]
    [InlineData(nameof(OutboxOptions<>.RetryDelays), "retry delays must not be null")]
    [InlineData("NegativeRetryDelay", "retry delay must not be negative")]
    [InlineData(nameof(OutboxOptions<>.MaxPayloadLength), "maximum payload length")]
    [InlineData(nameof(OutboxOptions<>.Retention), "retention")]
    [InlineData(nameof(OutboxOptions<>.SerializerOptions), "serializer options")]
    public async Task Invalid_options_fail_the_application_at_startup_naming_the_context(string invalid, string expectedFailure)
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(
            options => MakeInvalid(options, invalid),
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Act
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        // Assert
        var failure = Assert.Single(exception.Failures);
        Assert.Contains(expectedFailure, failure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(RegistrationDbContext), failure, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_options_of_one_outbox_do_not_blame_another()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutbox<OtherDbContext>(
            options => options.BatchSize = 0,
            config => config.WithMessage<SecondMessage, SecondMessageHandler>());
        using var host = builder.Build();

        // Act
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        // Assert
        var failure = Assert.Single(exception.Failures);
        Assert.Contains(nameof(OtherDbContext), failure, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(RegistrationDbContext), failure, StringComparison.Ordinal);
    }

    [Fact]
    public void Registered_message_types_are_recorded_with_their_message_types()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        var registry = host.Services.GetRequiredService<OutboxRegistry<RegistrationDbContext>>();
        var registration = Assert.Single(registry.Registrations);
        Assert.Equal(FirstMessage.MessageType, registration.MessageType);
        Assert.Equal(typeof(FirstMessage), registration.MessageClrType);
        Assert.True(registry.Contains(FirstMessage.MessageType));
    }

    private static void MakeInvalid(OutboxOptions<RegistrationDbContext> options, string invalid)
    {
        switch (invalid)
        {
            case nameof(OutboxOptions<>.BatchSize):
                options.BatchSize = 0;
                break;
            case nameof(OutboxOptions<>.LeaseDuration):
                options.LeaseDuration = TimeSpan.Zero;
                break;
            case nameof(OutboxOptions<>.RetryDelays):
                options.RetryDelays = null!;
                break;
            case "NegativeRetryDelay":
                options.RetryDelays = [TimeSpan.FromSeconds(-1)];
                break;
            case nameof(OutboxOptions<>.MaxPayloadLength):
                options.MaxPayloadLength = 0;
                break;
            case nameof(OutboxOptions<>.Retention):
                options.Retention = TimeSpan.Zero;
                break;
            case nameof(OutboxOptions<>.SerializerOptions):
                options.SerializerOptions = null!;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalid), invalid, "Unknown invalid option.");
        }
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<RegistrationDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.Services.AddDbContext<OtherDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        return builder;
    }

    private sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
        }
    }

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OtherOutboxMessage");
        }
    }

    private sealed record FirstMessage : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "registration.first.v1";
    }

    private sealed record ClashingMessage : IOutboxMessage
    {
        // Deliberately the same key as FirstMessage
        public static OutboxMessageType MessageType => "registration.first.v1";
    }

    private sealed record ShoutingMessage : IOutboxMessage
    {
        // Deliberately not lowercase
        public static OutboxMessageType MessageType => "REGISTRATION.FIRST.V1";
    }

    private sealed record SecondMessage : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "registration.second.v1";
    }

    private sealed class FirstMessageHandler : IOutboxMessageHandler<FirstMessage>
    {
        public Task HandleAsync(FirstMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class AnotherFirstMessageHandler : IOutboxMessageHandler<FirstMessage>
    {
        public Task HandleAsync(FirstMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ClashingMessageHandler : IOutboxMessageHandler<ClashingMessage>
    {
        public Task HandleAsync(ClashingMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ShoutingMessageHandler : IOutboxMessageHandler<ShoutingMessage>
    {
        public Task HandleAsync(ShoutingMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SecondMessageHandler : IOutboxMessageHandler<SecondMessage>
    {
        public Task HandleAsync(SecondMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
