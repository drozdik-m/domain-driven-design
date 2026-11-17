using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

/// <summary>
/// Verifies that a context with lazy-loading proxies can still map and use the outbox and the blob catalogue.
/// </summary>
public class LazyLoadingProxiesTests
{
    private static readonly DateTime s_now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task A_context_with_lazy_loading_proxies_stores_and_reads_back_outbox_messages_and_blobs()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<ProxiesDbContext>()
            .UseSqlite(connection)
            .UseLazyLoadingProxies()
            .Options;
        var message = OutboxMessage.Create("proxies.message.v1", new OutboxPayload("""{"Text":"a"}"""), s_now);
        var blob = Blob.Create(new CreateBlobParams
        {
            Key = BlobKey.Create("invoices", BlobId.New()),
            OriginalFileName = "invoice.pdf",
            ContentType = MediaType.OctetStream,
            Size = 3,
            CreatedAtUtc = s_now,
            Checksum = null,
            Metadata = BlobMetadata.Empty,
            CreatedBy = null,
            ExpiresAtUtc = null,
        });

        // Act
        await using (var context = new ProxiesDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Add(message);
            context.Add(blob);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new ProxiesDbContext(options))
        {
            var storedMessage = await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
            var storedBlob = await context.Set<Blob>().SingleAsync(TestContext.Current.CancellationToken);
            storedMessage.MarkProcessed(s_now);
            storedBlob.SetExpiry(s_now);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using (var context = new ProxiesDbContext(options))
        {
            var storedMessage = await context.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
            var storedBlob = await context.Set<Blob>().SingleAsync(TestContext.Current.CancellationToken);
            Assert.NotEqual(typeof(OutboxMessage), storedMessage.GetType());
            Assert.Equal(message.Id, storedMessage.Id);
            Assert.Equal(s_now, storedMessage.ProcessedAt);
            Assert.NotEqual(typeof(Blob), storedBlob.GetType());
            Assert.Equal(blob.Id, storedBlob.Id);
            Assert.Equal(s_now, storedBlob.ExpiresAt);
        }
    }

    private sealed class ProxiesDbContext(DbContextOptions<ProxiesDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutbox("OutboxMessage");
            modelBuilder.AddBlobs();
        }
    }
}
