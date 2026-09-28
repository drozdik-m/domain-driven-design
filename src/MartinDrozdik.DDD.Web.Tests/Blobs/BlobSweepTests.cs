using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Verifies that blobs which were only ever meant to be temporary go away once they expire.
/// </summary>
public class BlobSweepTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_blob_past_its_expiry_is_removed_with_its_content()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var added = await storage.AddOnSaveAsync(
            CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) },
            TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_blob_that_has_not_expired_yet_is_left_alone()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        await storage.AddOnSaveAsync(
            CreateRequest() with { ExpiresAt = s_initialTime.AddHours(5) },
            TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(1));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result.Expired);
        Assert.Equal(1, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_blob_with_no_expiry_is_kept_indefinitely()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromDays(3650));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result.Expired);
        Assert.Equal(1, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_row_whose_content_is_missing_is_left_alone_by_the_sweep()
    {
        // Arrange
        // Deciding a row is worthless is not the sweep's job, and cannot be undone
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await store.DeleteAsync(added.Value.Key, TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromDays(30));
        await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Content_of_an_expired_blob_that_cannot_be_removed_yet_is_retried_by_the_outbox()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var added = await storage.AddOnSaveAsync(
            CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) },
            TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        store.FailOn(added.Value.Key, new IOException("The file is locked."));

        time.Advance(TimeSpan.FromHours(2));
        await sweeper.SweepAsync(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        var lockedStillThere = store.TryRead(added.Value.Key, out _);

        // Act
        store.Heal(added.Value.Key);
        time.Advance(TimeSpan.FromDays(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(lockedStillThere);
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task One_sweep_works_through_a_backlog_larger_than_one_batch()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithTestingLogger(out var logger)
            .WithServices(services => services.Configure<BlobSweepOptions>(options => options.BatchSize = 2))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var keys = new List<BlobKey>();
        for (var i = 0; i < 5; i++)
        {
            var added = await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
            keys.Add(added.Value.Key);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        // The content of every pass is removed, not only that of the first one
        Assert.Equal(5, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        foreach (var key in keys)
        {
            Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
        }

        Assert.Empty(logger.At(LogLevel.Warning));
    }

    [Fact]
    public async Task A_backlog_of_exactly_one_batch_is_swept_without_warning()
    {
        // Arrange
        // The full page makes the sweep look again, and the empty one after it tells it that it caught up
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithTestingLogger(out var logger)
            .WithServices(services => services.Configure<BlobSweepOptions>(options =>
            {
                options.BatchSize = 2;
                options.MaxPasses = 2;
            }))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        for (var i = 0; i < 2; i++)
        {
            await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(logger.At(LogLevel.Warning));
    }

    [Fact]
    public async Task A_backlog_that_ends_exactly_on_the_last_pass_still_warns_it_may_be_behind()
    {
        // Arrange
        // A full last page cannot tell whether more is waiting behind it, so the sweep errs on the side of warning
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithTestingLogger(out var logger)
            .WithServices(services => services.Configure<BlobSweepOptions>(options =>
            {
                options.BatchSize = 2;
                options.MaxPasses = 2;
            }))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        for (var i = 0; i < 4; i++)
        {
            await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Single(logger.At(LogLevel.Warning));
    }

    [Fact]
    public async Task An_expired_blob_someone_decided_to_keep_meanwhile_is_left_alone()
    {
        // Arrange
        // The sweep loads the expired row, and before it saves, another instance clears its expiry
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithServices(CompetingOutbox.Keeping)
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var added = await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        // Both the row and its content survive, the delete message was taken back with the row
        Assert.Equal(0, result.Expired);
        var kept = await context.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(kept.ExpiresAt);
        Assert.True(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_expired_blob_tagged_meanwhile_is_swept_by_a_later_pass()
    {
        // Arrange
        // The first pass loses the row to a concurrent tag, and the next pass loads it again with its new stamp
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithTestingLogger(out var logger)
            .WithServices(CompetingOutbox.Tagging)
            .WithServices(services => services.Configure<BlobSweepOptions>(options => options.BatchSize = 1))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        var added = await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        // The first pass did lose the row, so it was a later pass that removed it
        Assert.Contains(logger.At(LogLevel.Information), entry => entry.Message.Contains("let go", StringComparison.Ordinal));
        Assert.Equal(1, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_sweep_stops_after_the_maximum_number_of_passes_and_warns_it_is_behind()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithTestingLogger(out var logger)
            .WithServices(services => services.Configure<BlobSweepOptions>(options =>
            {
                options.BatchSize = 2;
                options.MaxPasses = 2;
            }))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        for (var i = 0; i < 5; i++)
        {
            await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, result.Expired);
        Assert.Equal(1, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Single(logger.At(LogLevel.Warning));
    }

    [Fact]
    public async Task An_expired_blob_someone_else_removed_first_does_not_end_the_sweep()
    {
        // Arrange
        // The sweep loads the expired rows, and before it saves, another instance deletes one of them
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithServices(CompetingOutbox.Deleting)
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Expired);
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_conflict_on_a_row_the_sweep_did_not_remove_is_rethrown()
    {
        // Arrange
        // The caller tracks a delete of a blob that is not expired, and another instance deletes it first
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        await storage.AddOnSaveAsync(CreateRequest() with { ExpiresAt = s_initialTime.AddHours(1) }, TestContext.Current.CancellationToken);
        var kept = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var foreign = await context.Set<Blob>().SingleAsync(b => b.Id == kept.Value.Id, TestContext.Current.CancellationToken);
        context.Set<Blob>().Remove(foreign);
        await context.Set<Blob>().Where(b => b.Id == kept.Value.Id).ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromHours(2));
        var sweep = () => sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(sweep);
    }

    [Fact]
    public async Task A_sweep_with_nothing_to_do_reports_nothing()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).Build();
        using var scope = app.Services.CreateScope();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        // Act
        var result = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BlobSweepResult.Empty, result);
    }

    /// <summary>
    /// Builds a request storing a small file.
    /// </summary>
    /// <returns>The request.</returns>
    private static BlobUploadRequest CreateRequest()
        => new()
        {
            Container = TestBlobContainers.Invoices,
            Content = new MemoryStream(Encoding.UTF8.GetBytes("hello")),
            OriginalFileName = "report.pdf",
            ContentType = "application/pdf",
        };

    /// <summary>
    /// An outbox that lets another instance act on the first blob it is asked to remove, just before the sweep saves.
    /// </summary>
    /// <param name="inner">The outbox doing the actual work.</param>
    /// <param name="services">The application's services, to reach the catalogue as the other instance would.</param>
    /// <param name="compete">What the other instance does to the blob, in a context of its own.</param>
    private sealed class CompetingOutbox(
        IOutbox<TestDbContext> inner,
        IServiceProvider services,
        Action<TestDbContext, BlobId> compete) : IOutbox<TestDbContext>
    {
        private bool _competed;

        /// <summary>
        /// Another instance deletes the blob.
        /// </summary>
        /// <param name="services">The services of the application under test.</param>
        public static void Deleting(IServiceCollection services)
            => Register(services, (context, id) => context.Set<Blob>().Where(b => b.Id == id).ExecuteDelete());

        /// <summary>
        /// Another instance decides to keep the blob indefinitely.
        /// </summary>
        /// <param name="services">The services of the application under test.</param>
        public static void Keeping(IServiceCollection services)
            => Register(services, (context, id) =>
            {
                context.Set<Blob>().Single(b => b.Id == id).SetExpiry(null);
                context.SaveChanges();
            });

        /// <summary>
        /// Another instance tags the blob, which leaves it expired.
        /// </summary>
        /// <param name="services">The services of the application under test.</param>
        public static void Tagging(IServiceCollection services)
            => Register(services, (context, id) =>
            {
                context.Set<Blob>().Single(b => b.Id == id).SetMetadata("scan_state", "clean").IsSuccess();
                context.SaveChanges();
            });

        /// <summary>
        /// Wraps the registered outbox.
        /// </summary>
        /// <param name="services">The services of the application under test.</param>
        /// <param name="compete">What the other instance does to the blob.</param>
        private static void Register(IServiceCollection services, Action<TestDbContext, BlobId> compete)
        {
            var registered = services.Single(d => d.ServiceType == typeof(IOutbox<TestDbContext>));
            services.Remove(registered);
            services.AddScoped<IOutbox<TestDbContext>>(provider => new CompetingOutbox(
                (IOutbox<TestDbContext>)ActivatorUtilities.CreateInstance(provider, registered.ImplementationType!),
                provider,
                compete));
        }

        /// <inheritdoc />
        public OutboxTicket AddOnSave<TMessage>(TMessage message, OutboxMessageSettings? settings = null)
            where TMessage : IOutboxMessage
        {
            if (!_competed && message is DeleteBlobMessage delete)
            {
                _competed = true;
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                compete(context, new BlobId(Guid.Parse(delete.BlobId)));
            }

            return inner.AddOnSave(message, settings);
        }

        /// <inheritdoc />
        public Task<OutboxTicket> AddNowAsync<TMessage>(TMessage message, OutboxMessageSettings? settings, CancellationToken cancellationToken)
            where TMessage : IOutboxMessage
            => inner.AddNowAsync(message, settings, cancellationToken);

        /// <inheritdoc />
        public Task<bool> RemoveOnSaveAsync(OutboxTicket ticket, CancellationToken cancellationToken)
            => inner.RemoveOnSaveAsync(ticket, cancellationToken);

        /// <inheritdoc />
        public Task<bool> RemoveNowAsync(OutboxTicket ticket, CancellationToken cancellationToken)
            => inner.RemoveNowAsync(ticket, cancellationToken);

        /// <inheritdoc />
        public Task<OutboxTicket?> PostponeOnSaveAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken)
            => inner.PostponeOnSaveAsync(ticket, availableAt, cancellationToken);

        /// <inheritdoc />
        public Task<OutboxTicket?> PostponeNowAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken)
            => inner.PostponeNowAsync(ticket, availableAt, cancellationToken);
    }
}
