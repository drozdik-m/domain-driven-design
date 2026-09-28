using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Outbox;

/// <summary>
/// Base class for smoke tests of the outbox, verifying that it is wired into the application correctly.
/// </summary>
/// <remarks>
/// These are wiring checks only.
/// They deliberately never deliver anything - dispatching an arbitrary application's messages could send real mail.
/// To assert on delivery, write a test calling <see cref="OutboxTestExtensions.ProcessOutboxAsync(ITestedApp, CancellationToken)"/>.
/// </remarks>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
/// <typeparam name="TDbContext">The context the outbox is added over.</typeparam>
public abstract class OutboxSmokeTests<TProgram, TDbContext> : IDisposable
    where TProgram : class
    where TDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSmokeTests{TProgram, TDbContext}"/> class.
    /// </summary>
    /// <param name="builder">App builder under test.</param>
    protected OutboxSmokeTests(TestedAppBuilder<TProgram> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        App = builder.Build();
    }

    /// <summary>
    /// Gets the application under test, for derived classes adding tests of their own.
    /// </summary>
    protected TestedApp<TProgram> App { get; }

    /// <summary>
    /// Verifies messages can be enqueued over <typeparamref name="TDbContext"/>.
    /// </summary>
    [Fact]
    public void Outbox_resolves_with_all_its_dependencies()
    {
        // Arrange
        using var scope = App.Services.CreateScope();

        // Act
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<TDbContext>>();

        // Assert
        Assert.NotNull(outbox);
    }

    /// <summary>
    /// Verifies pending messages can be delivered.
    /// </summary>
    [Fact]
    public void Outbox_processor_resolves_with_all_its_dependencies()
    {
        // Arrange
        using var scope = App.Services.CreateScope();

        // Act
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        // Assert
        Assert.NotNull(processor);
    }

    /// <summary>
    /// Verifies the options of the outbox pass the validation of the application.
    /// </summary>
    [Fact]
    public void Outbox_options_are_valid()
    {
        // Arrange
        var validations = App.Services.GetServices<IValidateOptions<OutboxOptions>>();
        Assert.NotEmpty(validations);

        // Act
        // Resolving the options runs every registered validation
        var options = App.Services.GetRequiredService<IOptions<OutboxOptions>>().Value;

        // Assert
        App.TestOutputHelper.WriteLine($"Outbox: batch size={options.BatchSize}, lease={options.LeaseDuration}, attempts={options.RetryDelays.Count + 1}, max payload={options.MaxPayloadLength?.ToString() ?? "unlimited"}, retention={options.Retention?.ToString() ?? "forever"}");
    }

    /// <summary>
    /// Verifies every registered message type has a handler that can actually be constructed.
    /// A handler missing a dependency only fails once a real message is dispatched, where the failure is swallowed into a retry.
    /// </summary>
    [Fact]
    public void Every_registered_message_type_has_a_resolvable_handler()
    {
        // Arrange
        var registry = App.Services.GetRequiredService<OutboxRegistry>();
        using var scope = App.Services.CreateScope();

        // Act
        var registrations = registry.Registrations;

        // Assert
        Assert.NotEmpty(registrations);
        foreach (var registration in registrations)
        {
            App.TestOutputHelper.WriteLine($"{registration.MessageType} -> {registration.HandlerServiceType.Name}");

            var handler = scope.ServiceProvider.GetService(registration.HandlerServiceType);
            Assert.True(handler is not null, $"Outbox message type '{registration.MessageType}' has no resolvable handler.");
        }
    }

    /// <summary>
    /// Disposes the application under test.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            App.Dispose();
        }
    }
}
