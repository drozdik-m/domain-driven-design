using System.Collections.Concurrent;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Runs several application instances over one outbox table.
/// </summary>
/// <remarks>
/// Every instance is a plain host of its own, with its own container and registry, over one shared SQLite file.
/// Its context is registered the way production registers it: without <see cref="DbContextOptionsBuilder.EnableDetailedErrors(bool)"/>,
/// which the Testing environment of the test application turns on.
/// The clock, the log and the record of handled messages are shared, so a test can drive and observe all instances.
/// </remarks>
public abstract class OutboxInstancesTestBase : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_outbox_instances.db");
    private readonly List<IHost> _instances = [];

    /// <summary>
    /// Gets the time the shared clock starts at.
    /// </summary>
    protected static DateTimeOffset InitialTime { get; } = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets the clock every instance uses unless it is given one of its own.
    /// </summary>
    protected FakeTimeProvider Time { get; } = new(InitialTime);

    /// <summary>
    /// Gets the record of handled messages, shared by the handlers of every instance.
    /// </summary>
    protected ReviewHandlerHooks Hooks { get; } = new();

    /// <summary>
    /// Gets the log of every instance.
    /// </summary>
    protected TestLogger Logger { get; } = new();

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Enqueues messages through an instance and commits them in one save.
    /// </summary>
    /// <typeparam name="TMessage">The type of the messages.</typeparam>
    /// <param name="instance">The instance to enqueue through.</param>
    /// <param name="messages">The messages to enqueue.</param>
    /// <returns>A task that completes once the messages are committed.</returns>
    protected static async Task EnqueueAsync<TMessage>(IHost instance, IEnumerable<TMessage> messages)
        where TMessage : IOutboxMessage
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>();
        foreach (var message in messages)
        {
            outbox.AddOnSave(message);
        }

        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Enqueues a message through an instance and commits it.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="instance">The instance to enqueue through.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <returns>The ticket of the committed message.</returns>
    protected static async Task<OutboxTicket> EnqueueWithTicketAsync<TMessage>(IHost instance, TMessage message)
        where TMessage : IOutboxMessage
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var ticket = scope.ServiceProvider.GetRequiredService<IOutbox<ReviewDbContext>>().AddOnSave(message);
        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().SaveChangesAsync(TestContext.Current.CancellationToken);
        return ticket;
    }

    /// <summary>
    /// Runs one iteration of the dispatch task of an instance in a scope of its own, as the recurring task host does.
    /// </summary>
    /// <param name="instance">The instance whose dispatch task runs.</param>
    /// <returns>A task that completes with the iteration.</returns>
    protected static async Task RunDispatchIterationAsync(IHost instance)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        var task = new OutboxDispatchRecurringTask<ReviewDbContext>(scope.ServiceProvider.GetRequiredService<IOutboxProcessor<ReviewDbContext>>());
        await task.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads every row of the outbox table, untracked.
    /// </summary>
    /// <param name="instance">The instance to read through.</param>
    /// <returns>Every row of the table.</returns>
    protected static async Task<List<OutboxMessage>> GetMessagesAsync(IHost instance)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ReviewDbContext>()
            .Set<OutboxMessage>()
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Disposes every instance and deletes the shared database.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (var instance in _instances)
        {
            instance.Dispose();
        }

        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Left to the OS temp cleanup
        }
    }

    /// <summary>
    /// Builds an instance over the shared database and creates the database if it does not exist yet.
    /// </summary>
    /// <param name="configure">The message types of its outbox.</param>
    /// <param name="configureOptions">Its outbox options, with <see cref="OutboxOptions{TDbContext}.ClockSkewTolerance"/> set to zero beforehand.</param>
    /// <param name="configureContext">Its context options, e.g. no tracking by default.</param>
    /// <param name="detectChangesAutomatically">Whether its context detects changes automatically.</param>
    /// <param name="time">Its own clock, instead of <see cref="Time"/>.</param>
    /// <param name="interceptor">An interceptor added to its context.</param>
    /// <returns>The instance, not started.</returns>
    protected async Task<IHost> CreateInstanceAsync(
        Action<OutboxConfig> configure,
        Action<OutboxOptions<ReviewDbContext>>? configureOptions = null,
        Action<DbContextOptionsBuilder>? configureContext = null,
        bool detectChangesAutomatically = true,
        TimeProvider? time = null,
        IInterceptor? interceptor = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(time ?? Time);
        builder.Services.AddSingleton(Hooks);
        builder.Services.AddSingleton<ILoggerProvider>(Logger);
        builder.Services.AddSingleton(new ReviewContextSettings(detectChangesAutomatically));
        builder.Services.AddDbContext<ReviewDbContext>(options =>
        {
            options.UseSqlite($"Data Source={_databasePath}");
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }

            configureContext?.Invoke(options);
        });
        builder.AddOutbox<ReviewDbContext>(
            options =>
            {
                // The instances share one clock unless a test gives one its own, so a lease ends exactly when it says
                options.ClockSkewTolerance = TimeSpan.Zero;
                configureOptions?.Invoke(options);
            },
            configure);

        var instance = builder.Build();
        _instances.Add(instance);

        await using var scope = instance.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ReviewDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return instance;
    }

    /// <summary>
    /// The context of every instance.
    /// </summary>
    protected sealed class ReviewDbContext : DbContext
    {
        public ReviewDbContext(DbContextOptions<ReviewDbContext> options, ReviewContextSettings settings)
            : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = settings.DetectChangesAutomatically;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
        }
    }

    /// <summary>
    /// How the context of an instance tracks its entities, set in its constructor as an application would.
    /// </summary>
    /// <param name="DetectChangesAutomatically">The value of <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled"/>.</param>
    protected sealed record ReviewContextSettings(bool DetectChangesAutomatically);

    /// <summary>
    /// What the handlers of every instance record and do.
    /// </summary>
    protected sealed class ReviewHandlerHooks
    {
        public ConcurrentQueue<string> Handled { get; } = [];

        public Func<string, CancellationToken, Task> OnHandleAsync { get; set; } = static (_, _) => Task.CompletedTask;
    }

    protected sealed record ReviewMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "review.message.v1";
    }

    protected sealed class ReviewMessageHandler(ReviewHandlerHooks hooks) : IOutboxMessageHandler<ReviewMessage>
    {
        public async Task HandleAsync(ReviewMessage message, CancellationToken cancellationToken)
        {
            hooks.Handled.Enqueue(message.Text);
            await hooks.OnHandleAsync(message.Text, cancellationToken);
        }
    }
}
