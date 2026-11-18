using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Signatures;
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest());

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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest());

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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest());
        context.SomeEntities.Add(new SomeAggregateRoot());
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(originalFileName: "Faktura 2026 (final).pdf"));
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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var first = await storage.UploadAsync(context, CreateRequest());
        var second = await storage.UploadAsync(context, CreateRequest());

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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.UploadAsync(context, CreateRequest());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await store.DeleteAsync(added.Value.Key, TestContext.Current.CancellationToken);

        // Act
        var result = await storage.OpenReadAsync(context, added.Value.Id, TestContext.Current.CancellationToken);

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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        var added = await storage.UploadAsync(context, CreateRequest());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await storage.DeleteOnSaveAsync(context, added.Value, TestContext.Current.CancellationToken);

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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.GetAsync(context, BlobId.New(), TestContext.Current.CancellationToken);

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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        var added = await storage.UploadAsync(
            context,
            CreateRequest() with { Metadata = BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value });
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        var added = await storage.UploadAsync(context, CreateRequest());
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(originalFileName: "README"));
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest());
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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        await storage.UploadAsync(context, CreateRequest());
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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(originalFileName: "README"));

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
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var upload = () => storage.UploadAsync(context, CreateRequest(container: "invoice"));

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(upload);
        Assert.Equal(0, store.Count);
        Assert.Equal(0, await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Preparing_an_upload_writes_nothing_until_the_caller_saves()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var upload = storage.PrepareUpload(context, CreateRequest());
        var beforeSave = await context.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        upload.IsSuccess();
        Assert.Equal(0, beforeSave);
        var cleanup = await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(upload.Value.Cleanup, cleanup.ToTicket());
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task Writing_the_content_before_its_cleanup_is_saved_is_refused()
    {
        // Arrange
        // A rollback would take the unsaved cleanup with it and leave the content behind for good
        using var app = new TestedWebAppBuilder(testOutputHelper).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var upload = storage.PrepareUpload(context, CreateRequest());

        // Act
        var write = () => storage.AddOnSaveAsync(context, upload.Value, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(write);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task An_upload_through_a_context_pointed_at_another_database_stays_in_that_database()
    {
        // Arrange
        // E.g. a database per tenant, chosen by a connection string set on the context at runtime
        var tenantBuilder = new TestedWebAppBuilder(testOutputHelper);
        using var tenant = tenantBuilder.Build();
        _ = tenant.Services; // starts the app, which creates its database

        using var app = new TestedWebAppBuilder(testOutputHelper).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Database.SetConnectionString(tenantBuilder.ConnectionString);
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // The blob is catalogued in the tenant's database, and its cleanup was cancelled there
        result.IsSuccess();
        using var tenantScope = tenant.Services.CreateScope();
        var tenantContext = tenantScope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(1, await tenantContext.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await tenantContext.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));

        // Nothing reached the registered database
        using var registeredScope = app.Services.CreateScope();
        var registeredContext = registeredScope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(0, await registeredContext.Set<Blob>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await registeredContext.Set<OutboxMessage>().AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_options_of_a_container_apply_to_no_other_container()
    {
        // Arrange
        // Only the avatars carry a size limit, and the content is larger than it
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var avatar = await storage.UploadAsync(context, CreateRequest(container: TestBlobContainers.Avatars));
        var invoice = await storage.UploadAsync(context, CreateRequest(container: TestBlobContainers.Invoices));

        // Assert
        avatar.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobTooLarge, avatar.Error.Code);
        invoice.IsSuccess();
    }

    [Fact]
    public async Task A_file_that_does_not_start_like_its_declared_type_is_refused_before_anything_is_written()
    {
        // Arrange
        // HTML declared as a PDF would otherwise be served back as application/pdf
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.ContentSignatures = ContentSignature.Common))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        var store = (InMemoryBlobStore)scope.ServiceProvider.GetRequiredService<IBlobStore>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(content: "<html><body>not a pdf</body></html>"));

        // Assert
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobContentMismatch, result.Error.Code);
        Assert.Equal(0, store.Count);
        Assert.Empty(context.Set<Blob>().Local);
    }

    [Fact]
    public async Task An_empty_file_declared_as_a_type_with_a_signature_is_refused()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.ContentSignatures = ContentSignature.Common))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(content: string.Empty));

        // Assert
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobContentMismatch, result.Error.Code);
    }

    [Fact]
    public async Task A_file_that_starts_like_its_declared_type_is_stored_whole()
    {
        // Arrange
        // The check reads the first bytes off a stream that cannot be rewound, so they have to reach the store too
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.ContentSignatures = ContentSignature.Common))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();
        const string pdf = "%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF";

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(content: pdf, forwardOnly: true));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess();
        Assert.Equal(Encoding.UTF8.GetByteCount(pdf), result.Value.Size);
        await using var read = (await storage.OpenReadAsync(context, result.Value.Id, TestContext.Current.CancellationToken)).Value;
        using var reader = new StreamReader(read.Content);
        Assert.Equal(pdf, await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void A_file_of_a_type_no_signature_lists_is_refused_before_its_cleanup_is_tracked()
    {
        // Arrange
        // The signatures are an allow-list, and the declaration alone is enough to refuse
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.ContentSignatures = ContentSignature.Common))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = storage.PrepareUpload(context, CreateRequest(originalFileName: "notes.txt", contentType: "text/plain"));

        // Assert
        result.IsFailure();
        Assert.Equal(BlobErrorCodes.BlobTypeNotAllowed, result.Error.Code);
        Assert.Empty(context.Set<OutboxMessage>().Local);
    }

    [Fact]
    public async Task A_file_of_a_type_listed_with_a_lone_wildcard_is_stored_whatever_it_starts_with()
    {
        // Arrange
        // The way to allow a format that has no signature, such as plain text
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithInMemoryBlobStore()
            .WithServices(services => services.Configure<BlobContainerOptions>(
                TestBlobContainers.Invoices,
                options => options.ContentSignatures = [.. ContentSignature.Common, new ContentSignature("text/plain", "??")]))
            .Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(originalFileName: "notes.txt", contentType: "text/plain"));

        // Assert
        result.IsSuccess();
        Assert.Equal("text/plain", result.Value.ContentType.Value);
    }

    [Fact]
    public async Task Without_signatures_a_file_is_stored_as_whatever_it_was_declared()
    {
        // Arrange
        // The check is opt-in
        using var app = new TestedWebAppBuilder(testOutputHelper).WithInMemoryBlobStore().Build();
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage<TestDbContext>>();

        // Act
        var result = await storage.UploadAsync(context, CreateRequest(content: "<html></html>"));

        // Assert
        result.IsSuccess();
        Assert.Equal("application/pdf", result.Value.ContentType.Value);
    }

    /// <summary>
    /// Builds a request storing a small file.
    /// </summary>
    /// <param name="originalFileName">The name it arrived with, or null for the default.</param>
    /// <param name="container">The container to store into, or null for the default.</param>
    /// <param name="content">The text to store, or null for the default.</param>
    /// <param name="contentType">The declared media type, or null for <c>application/pdf</c>.</param>
    /// <param name="forwardOnly">True to hand over a stream that cannot seek, like a request body.</param>
    /// <returns>The request.</returns>
    private static BlobUploadRequest CreateRequest(
        string? originalFileName = null,
        string? container = null,
        string? content = null,
        string? contentType = null,
        bool forwardOnly = false)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? "hello");

        return new()
        {
            Container = container ?? TestBlobContainers.Invoices,
            Content = forwardOnly ? new ForwardOnlyStream(bytes) : new MemoryStream(bytes),
            OriginalFileName = originalFileName ?? "report.pdf",
            ContentType = contentType ?? "application/pdf",
        };
    }

    /// <summary>
    /// A stream that hands out a few bytes per read and cannot seek, like a request body.
    /// </summary>
    /// <param name="bytes">The content.</param>
    private sealed class ForwardOnlyStream(byte[] bytes) : MemoryStream(bytes)
    {
        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => base.ReadAsync(buffer[..Math.Min(buffer.Length, 3)], cancellationToken);
    }
}
