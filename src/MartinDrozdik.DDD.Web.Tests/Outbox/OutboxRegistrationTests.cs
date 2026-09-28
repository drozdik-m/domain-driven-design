using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox;
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
    public void Registering_the_same_message_type_twice_throws_naming_both_types()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<RegistrationDbContext>(
                configureOptions: null,
                config => config
                    .WithMessage<FirstMessage, FirstMessageHandler>()
                    .WithMessage<ClashingMessage, ClashingMessageHandler>()));

        // Assert
        Assert.Contains(nameof(FirstMessage), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ClashingMessage), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_the_outbox_over_a_second_context_throws_naming_both_contexts()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Act
        var exception = Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<OtherDbContext>(
                configureOptions: null,
                config => config.WithMessage<FirstMessage, FirstMessageHandler>()));

        // Assert
        Assert.Contains(nameof(RegistrationDbContext), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(OtherDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_the_outbox_twice_over_the_same_context_throws()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());

        // Act
        // Assert
        // The second registry would be dropped, and its message types with it
        Assert.Throws<OutboxException>(() =>
            builder.AddOutbox<RegistrationDbContext>(
                configureOptions: null,
                config => config.WithMessage<FirstMessage, FirstMessageHandler>()));
    }

    [Fact]
    public void The_outbox_resolves_only_over_the_context_it_was_added_over()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.Services.AddDbContext<OtherDbContext>(options => options.UseSqlite("Data Source=:memory:"));

        // Act
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IOutbox<RegistrationDbContext>>());
        Assert.Null(scope.ServiceProvider.GetService<IOutbox<OtherDbContext>>());
    }

    [Fact]
    public void The_engine_registers_without_any_background_loop()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        // The processor is there, so another scheduler could drive it
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IOutboxProcessor>());
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
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        builder.AddOutboxDispatchRecurringTask(options => options.Period = TimeSpan.FromSeconds(30));
        using var host = builder.Build();

        // Assert
        Assert.NotEmpty(host.Services.GetServices<IHostedService>());
        var schedule = host.Services.GetRequiredService<IOptions<RecurringTaskOptions<OutboxDispatchRecurringTask>>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(30), schedule.Period);
    }

    [Fact]
    public void The_trigger_is_registered_even_without_the_dispatch_task()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // No dispatch task, as an application driving the processor from Quartz would do
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        // The save interceptor always has something to poke, so triggering is a harmless no-op
        var trigger = host.Services.GetService<IRecurringTaskTrigger<OutboxDispatchRecurringTask>>();
        Assert.NotNull(trigger);
        trigger.Trigger();
    }

    [Fact]
    public async Task Invalid_options_fail_the_application_at_startup()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<RegistrationDbContext>(
            options => options.BatchSize = 0,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Act
        // Assert
        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Registered_message_types_are_recorded_with_their_handlers()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddOutbox<RegistrationDbContext>(
            configureOptions: null,
            config => config.WithMessage<FirstMessage, FirstMessageHandler>());
        using var host = builder.Build();

        // Assert
        var registry = host.Services.GetRequiredService<OutboxRegistry>();
        var registration = Assert.Single(registry.Registrations);
        Assert.Equal(FirstMessage.MessageType, registration.MessageType);
        Assert.Equal(typeof(FirstMessage), registration.MessageClrType);
        Assert.Equal(typeof(IOutboxMessageHandler<FirstMessage>), registration.HandlerServiceType);
        Assert.True(registry.Contains(FirstMessage.MessageType));
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<RegistrationDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        return builder;
    }

    private sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox();
        }
    }

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options);

    private sealed record FirstMessage : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "registration.first.v1";
    }

    private sealed record ClashingMessage : IOutboxMessage
    {
        // Deliberately the same key as FirstMessage
        public static OutboxMessageType MessageType => "registration.first.v1";
    }

    private sealed class FirstMessageHandler : IOutboxMessageHandler<FirstMessage>
    {
        public Task HandleAsync(FirstMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ClashingMessageHandler : IOutboxMessageHandler<ClashingMessage>
    {
        public Task HandleAsync(ClashingMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
