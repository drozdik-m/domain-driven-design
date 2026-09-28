using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Verifies that the façade joins the caller's transaction, and that drift between the catalogue and the store
/// is tolerated rather than reported.
/// </summary>
public class BlobStorageTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Adding_a_blob_writes_no_row_until_the_caller_saves()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess();
        var stored = await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, stored);
    }

    [Fact]
    public async Task Adding_a_blob_writes_its_content_before_the_caller_saves()
    {
        // Arrange
        // The order is deliberate: a rollback then leaves content nobody claims, which the sweep collects,
        // rather than a row pointing at nothing, which nothing ever cleans up
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        var blob = result.Value;
        Assert.True(await store.ExistsAsync(blob.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Saving_writes_the_blob_together_with_the_aggregate_that_produced_it()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        context.SomeEntities.Add(new SomeAggregateRoot());
        var result = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var blob = await context.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(result.Value.Id, blob.Id);
        Assert.Equal(1, await context.SomeEntities.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_stored_blob_records_what_was_actually_written()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(originalFileName: "Faktura 2026 (final).pdf"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var blob = result.Value;
        Assert.Equal("Faktura 2026 (final).pdf", blob.OriginalFileName);
        Assert.Equal("Faktura-2026-(final).pdf", blob.Name.Value);
        Assert.Equal("pdf", blob.Extension);
        Assert.Equal(Encoding.UTF8.GetByteCount("hello"), blob.Size);
        Assert.NotNull(blob.Checksum);
    }

    [Fact]
    public async Task Two_files_of_one_name_never_collide()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var first = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        var second = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(first.Value.Name, second.Value.Name);
        Assert.NotEqual(first.Value.Key, second.Value.Key);
    }

    [Fact]
    public async Task Reading_a_blob_whose_content_was_removed_behind_our_back_fails_and_keeps_the_row()
    {
        // Arrange
        // The store may only be unreachable, and metadata thrown away cannot be recovered
        using var app = new TestedWebAppBuilder(testOutputHelper).WithTestingLogger(out var logger).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await store.DeleteAsync(added.Value.Key, TestContext.Current.CancellationToken);

        // Act
        var result = await storage.OpenReadAsync(added.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobContentMissing, result.Error.Code);
        Assert.NotEmpty(logger.At(Microsoft.Extensions.Logging.LogLevel.Warning));
        Assert.Equal(1, await context.Set<Blob>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Enqueueing_a_deletion_removes_the_row_and_the_message_in_one_transaction()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await storage.DeleteOnSaveAsync(added.Value, TestContext.Current.CancellationToken);

        // Assert
        // Nothing has happened yet - the row, the message and the content are all still exactly as they were
        Assert.Equal(1, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, await context.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));

        // Still there: the content only goes once the outbox delivers the message
        Assert.True(await store.ExistsAsync(added.Value.Key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reading_a_blob_nothing_is_catalogued_under_fails()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.GetAsync(BlobId.New(), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobNotFound, result.Error.Code);
    }

    [Fact]
    public async Task Metadata_survives_a_round_trip_through_the_database()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        var added = await storage.AddOnSaveAsync(
            CreateRequest() with { Metadata = BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value },
            TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var reloaded = await context.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(added.Value.Metadata, reloaded.Metadata);
        Assert.Equal("1920", reloaded.Metadata["width"]);
    }

    [Fact]
    public async Task An_edit_to_the_metadata_is_detected_as_a_change_worth_saving()
    {
        // Arrange
        // A value comparer that only checked references would report this as unchanged and silently drop it
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        var added = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        added.Value.SetMetadata("scan_state", "clean").IsSuccess();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        using var freshScope = app.Services.CreateScope();
        var freshContext = freshScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var reloaded = await freshContext.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("clean", reloaded.Metadata["scan_state"]);
    }

    [Fact]
    public async Task A_stored_blob_records_no_extension_when_the_file_arrived_without_one()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(originalFileName: "README"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Absent is null, not an empty string - the column has nothing to say about a name that carries no extension
        Assert.Null(result.Value.Name.Extension);
        using var freshScope = app.Services.CreateScope();
        var freshContext = freshScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var reloaded = await freshContext.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(reloaded.Extension);
    }

    [Fact]
    public async Task A_stored_checksum_is_read_back_from_its_two_columns()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        using var freshScope = app.Services.CreateScope();
        var freshContext = freshScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var reloaded = await freshContext.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded.Checksum);
        Assert.Same(ChecksumAlgorithm.Sha256, reloaded.Checksum.Algorithm);
        Assert.Equal(result.Value.Checksum, reloaded.Checksum);
    }

    [Fact]
    public async Task A_blob_stored_without_hashing_is_read_back_without_a_checksum()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.Configure<BlobContainerOptions>(TestBlobContainers.Invoices, options => options.ComputeChecksum = false))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        await storage.AddOnSaveAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Both columns empty reads back as no checksum at all, not as one with empty parts
        using var freshScope = app.Services.CreateScope();
        var freshContext = freshScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var reloaded = await freshContext.Set<Blob>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(reloaded.Checksum);
    }

    [Fact]
    public async Task An_allow_list_refuses_a_file_that_arrived_without_an_extension()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.AllowedExtensions = new HashSet<string>(StringComparer.Ordinal) { "pdf" }))
            .Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var result = await storage.AddOnSaveAsync(CreateRequest(originalFileName: "README"), TestContext.Current.CancellationToken);

        // Assert
        // Nothing to match means nothing allows it
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobTypeNotAllowed, result.Error.Code);
    }

    [Fact]
    public async Task An_upload_to_a_container_nobody_registered_is_refused_before_anything_is_written()
    {
        // Arrange
        // A typo in a container name is a programming error, so it fails loudly rather than as a result
        using var app = new TestedWebAppBuilder(testOutputHelper).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var upload = () => storage.AddOnSaveAsync(CreateRequest(container: "invoice"), TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(upload);
        Assert.Equal(0, store.Count);
        Assert.Equal(0, await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_options_of_a_container_apply_to_no_other_container()
    {
        // Arrange
        // Only the avatars carry a size limit, and the content is larger than it
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Act
        var avatar = await storage.AddOnSaveAsync(CreateRequest(container: TestBlobContainers.Avatars), TestContext.Current.CancellationToken);
        var invoice = await storage.AddOnSaveAsync(CreateRequest(container: TestBlobContainers.Invoices), TestContext.Current.CancellationToken);

        // Assert
        avatar.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobTooLarge, avatar.Error.Code);
        invoice.IsSuccess();
    }

    /// <summary>
    /// Builds a request storing a small file.
    /// </summary>
    /// <param name="originalFileName">The name it arrived with, or null for the default.</param>
    /// <param name="container">The container to store into, or null for the default.</param>
    /// <returns>The request.</returns>
    private static BlobUploadRequest CreateRequest(string? originalFileName = null, string? container = null)
        => new()
        {
            Container = container ?? TestBlobContainers.Invoices,
            Content = new MemoryStream(Encoding.UTF8.GetBytes("hello")),
            OriginalFileName = originalFileName ?? "report.pdf",
            ContentType = "application/pdf",
        };
}
