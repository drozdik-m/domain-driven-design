using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Verifies the half of the module's atomicity story that runs the other way from deletion: content written by
/// a transaction that never committed is removed, and content whose row did commit never is.
/// </summary>
/// <remarks>
/// Every upload schedules the removal of its own content through the outbox before writing it, and cancels that
/// removal in the commit that catalogues the blob.
/// </remarks>
public class BlobOrphanTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Content_no_row_claims_is_removed_once_the_grace_period_has_passed()
    {
        // Arrange
        // Exactly what a rolled-back upload leaves behind: content was written, the row never committed
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Act
        time.Advance(GracePeriod(scope) + TimeSpan.FromMinutes(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Content_inside_the_grace_period_survives_even_though_no_row_claims_it_yet()
    {
        // Arrange
        // Between writing the content and committing the row, every legitimate upload looks like an orphan
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Act
        time.Advance(TimeSpan.FromMinutes(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_committed_upload_cancels_the_removal_of_its_content()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromDays(30));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, await CountMessagesAsync(context));
        Assert.True(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_upload_that_outlasts_the_grace_period_keeps_its_content()
    {
        // Arrange
        // A large upload over a slow link: by the time its last byte arrives, the removal scheduled when it
        // started would already be due, and the row has not been saved yet
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var uploadTime = GracePeriod(scope) + TimeSpan.FromMinutes(10);

        var added = await storage.AddOnSaveAsync(
            CreateRequest() with { Content = new SlowStream(Encoding.UTF8.GetBytes("hello"), time, uploadTime) },
            TestContext.Current.CancellationToken);

        // Act
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_upload_whose_transaction_outlasts_the_grace_period_is_refused_at_commit()
    {
        // Arrange
        // With a two-hour grace period, the caller keeps its transaction open for three hours, and meanwhile the
        // outbox removes the content as an orphan. Committing the row now would catalogue content that is gone.
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(TestBlobContainers.Invoices, o => o.OrphanGracePeriod = TimeSpan.FromHours(2)))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromHours(3));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        var commit = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(commit);
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
        using var other = app.Services.CreateScope();
        var otherContext = other.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(0, await otherContext.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_upload_whose_content_was_removed_while_it_arrived_fails_and_is_cleaned_up()
    {
        // Arrange
        // The upload takes longer than the grace period, and the removal runs before the last byte arrives
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var gracePeriod = GracePeriod(scope);
        var request = CreateRequest() with
        {
            Content = new InterruptedStream(Encoding.UTF8.GetBytes("hello"), async () =>
            {
                time.Advance(gracePeriod + TimeSpan.FromMinutes(1));
                await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
            }),
        };

        // Act
        var upload = () => storage.AddOnSaveAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<BlobUploadReclaimedException>(upload);

        // The content that did arrive is not lost track of - a second removal, due at once, takes it away
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, store.Count);
        Assert.Equal(0, await CountMessagesAsync(context));
    }

    [Fact]
    public async Task An_upload_refused_after_its_content_was_written_still_has_that_content_removed()
    {
        // Arrange
        // The blob is refused only once its content is in the store, and the caller then saves its other work anyway.
        // That save must not take the scheduled removal with it, or the content is orphaned for good.
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var request = CreateRequest() with { CreatedBy = new string('a', Blob.CreatedByMaxLength + 1) };

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => storage.AddOnSaveAsync(request, TestContext.Current.CancellationToken));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.Advance(GracePeriod(scope) + TimeSpan.FromMinutes(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, store.Count);
        Assert.Equal(0, await CountMessagesAsync(context));
    }

    [Fact]
    public async Task A_deleted_blob_whose_content_cannot_be_removed_right_now_is_retried_by_the_outbox()
    {
        // Arrange
        // The row is gone once the delete has saved, so the message is the only thing still leading to the content
        var time = new FakeTimeProvider(s_initialTime);
        using var app = new TestedWebAppBuilder(testOutputHelper).WithFakeTime(time).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await storage.DeleteOnSaveAsync(added.Value, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        store.FailOn(added.Value.Key, new IOException("The file is locked."));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Act
        store.Heal(added.Value.Key);
        time.Advance(TimeSpan.FromDays(1));
        await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.False(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Reads how long an upload has to commit its row.
    /// </summary>
    /// <param name="scope">The scope of the test.</param>
    /// <returns>The grace period.</returns>
    private static TimeSpan GracePeriod(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BlobContainerOptions>>().Get(TestBlobContainers.Invoices).OrphanGracePeriod;

    /// <summary>
    /// Counts the outbox messages still waiting to be delivered.
    /// </summary>
    /// <param name="context">The context to count through.</param>
    /// <returns>The number of pending messages.</returns>
    private static Task<int> CountMessagesAsync(TestDbContext context)
        => context.Set<OutboxMessage>().AsNoTracking().CountAsync(m => m.ProcessedAt == null, TestContext.Current.CancellationToken);

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
    /// Content that takes a while to arrive, moving a fake clock forward as it is read.
    /// </summary>
    /// <param name="content">The content to serve.</param>
    /// <param name="time">The clock to move.</param>
    /// <param name="duration">How long the whole content takes to arrive.</param>
    private sealed class SlowStream(byte[] content, FakeTimeProvider time, TimeSpan duration) : MemoryStream(content)
    {
        private bool _delayed;

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_delayed)
            {
                _delayed = true;
                time.Advance(duration);
            }

            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    /// <summary>
    /// Content during whose arrival something else happens, e.g. the outbox running on another instance.
    /// </summary>
    /// <param name="content">The content to serve.</param>
    /// <param name="meanwhile">What happens before the first byte is handed over.</param>
    private sealed class InterruptedStream(byte[] content, Func<Task> meanwhile) : MemoryStream(content)
    {
        private bool _interrupted;

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_interrupted)
            {
                _interrupted = true;
                await meanwhile();
            }

            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
