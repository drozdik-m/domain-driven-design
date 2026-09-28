using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Verifies that content is removed by the outbox, after the transaction that dropped its row committed.
/// </summary>
/// <remarks>
/// This is the whole reason blob deletion rides on the outbox: deleting a file and removing the row that
/// refers to it are writes against two systems, and only one of them can be rolled back.
/// </remarks>
public class BlobOutboxTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Delivering_the_message_removes_the_content()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var store = app.Services.GetRequiredService<IBlobStore>();
        BlobKey key;

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

            var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            key = added.Value.Key;
            await storage.DeleteOnSaveAsync(added.Value, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, dispatched);
        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delivering_the_message_twice_is_a_success_both_times()
    {
        // Arrange
        // At-least-once delivery makes this the normal case, not the strange one
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

            var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var message = added.Value.ToDeleteMessage();
            var outbox = scope.ServiceProvider.GetRequiredService<MartinDrozdik.DDD.Outbox.IOutbox<TestDbContext>>();

            // The same deletion, enqueued twice - what a redelivery amounts to
            outbox.AddOnSave(message);
            outbox.AddOnSave(message);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var dispatched = await app.DrainOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, dispatched);

        using var assertScope = app.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var deadLettered = await assertContext.Set<OutboxMessage>()
            .AsNoTracking()
            .CountAsync(m => m.FailedAt != null, TestContext.Current.CancellationToken);
        Assert.Equal(0, deadLettered);
    }

    [Fact]
    public async Task Delivering_a_deletion_for_content_that_is_already_gone_is_a_success()
    {
        // Arrange
        // Somebody cleared the folder by hand between the transaction committing and the message arriving
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<MartinDrozdik.DDD.Outbox.IOutbox<TestDbContext>>();

            outbox.AddOnSave(new DeleteBlobMessage("invoices", BlobId.New().ToString()));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, dispatched);

        using var assertScope = app.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var message = await assertContext.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.FailedAt);
    }

    [Fact]
    public async Task A_rolled_back_deletion_leaves_the_blob_and_its_content_untouched()
    {
        // Arrange
        // The point of enqueueing rather than deleting: nothing is true until the caller commits
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var store = app.Services.GetRequiredService<IBlobStore>();
        BlobKey key;

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

            var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            key = added.Value.Key;

            // Act
            await storage.DeleteOnSaveAsync(added.Value, TestContext.Current.CancellationToken);

            // ... and the transaction never commits
        }

        // Assert
        var dispatched = await app.ProcessOutboxAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, dispatched);
        Assert.True(await store.ExistsAsync(key, TestContext.Current.CancellationToken));

        using var assertScope = app.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(1, await assertContext.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
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
}
