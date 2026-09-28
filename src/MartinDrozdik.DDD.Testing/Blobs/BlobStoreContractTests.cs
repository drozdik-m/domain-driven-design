using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Blobs;

/// <summary>
/// The behaviour every <see cref="IBlobStore"/> has to show.
/// </summary>
/// <remarks>
/// <para>
/// Derive once per implementation.
/// </para>
/// <code>
/// public sealed class MyBlobStoreTests : BlobStoreContractTests
/// {
///     protected override IBlobStore CreateStore(TimeProvider timeProvider) =&gt; new MyBlobStore(timeProvider, ...);
/// }
/// </code>
/// <para>
/// The suite is mostly about what happens when something is <i>not</i> there, because that is the case applications get wrong and the case this module promises to survive.
/// </para>
/// </remarks>
public abstract class BlobStoreContractTests
{
    /// <summary>
    /// The SHA-256 of <see cref="Content"/>, to prove a store hashes the bytes it actually wrote.
    /// </summary>
    private const string ContentSha256 = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    private const string Content = "hello";

    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Written content reads back unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Written_content_reads_back_unchanged()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();

        // Act
        await WriteAsync(store, key, Content);

        // Assert
        Assert.Equal(Content, await ReadAsync(store, key));
    }

    /// <summary>
    /// Writing reports the size and the checksum of what was written.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Writing_reports_the_size_and_the_checksum_of_what_was_written()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();

        // Act
        var receipt = await WriteAsync(store, key, Content);

        // Assert
        Assert.Equal(Encoding.UTF8.GetByteCount(Content), receipt.Size);
        Assert.Equal(ContentSha256, receipt.Checksum?.Value);
    }

    /// <summary>
    /// Writing can skip the checksum.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Writing_can_skip_the_checksum()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();

        // Act
        var receipt = await WriteAsync(store, key, Content, new BlobWriteOptions(ComputeChecksum: false));

        // Assert
        Assert.Null(receipt.Checksum);
        Assert.Equal(Encoding.UTF8.GetByteCount(Content), receipt.Size);
    }

    /// <summary>
    /// Writing accepts a stream that cannot be rewound.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Writing_accepts_a_stream_that_cannot_be_rewound()
    {
        // Arrange
        // A request body is exactly this: forward only, and it does not know its own length
        var store = CreateStore();
        var key = CreateKey();
        await using var content = new ForwardOnlyStream(Encoding.UTF8.GetBytes(Content));

        // Act
        var receipt = await store.WriteAsync(key, content, BlobWriteOptions.Default, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Encoding.UTF8.GetByteCount(Content), receipt.Size);
        Assert.Equal(Content, await ReadAsync(store, key));
    }

    /// <summary>
    /// Writing refuses an address that is already taken.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Writing_refuses_an_address_that_is_already_taken()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();
        await WriteAsync(store, key, Content);

        // Act
        // Assert
        // Every address carries a fresh identity, so a collision is a bug rather than a retry
        await Assert.ThrowsAsync<BlobException>(() => WriteAsync(store, key, "other"));
    }

    /// <summary>
    /// Writing abandons content larger than the limit.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public Task Writing_abandons_content_larger_than_the_limit()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();

        // Act
        // Assert
        return Assert.ThrowsAsync<BlobTooLargeException>(() => WriteAsync(store, key, Content, new BlobWriteOptions(MaxSize: 2)));
    }

    /// <summary>
    /// Reading an address with nothing at it reports nothing.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Reading_an_address_with_nothing_at_it_reports_nothing()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var content = await store.OpenReadAsync(CreateKey(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(content);
    }

    /// <summary>
    /// Inspecting an address with nothing at it reports nothing.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Inspecting_an_address_with_nothing_at_it_reports_nothing()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var entry = await store.GetEntryAsync(CreateKey(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(entry);
    }

    /// <summary>
    /// Inspecting a written blob reports its size.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Inspecting_a_written_blob_reports_its_size()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();
        await WriteAsync(store, key, Content);

        // Act
        var entry = await store.GetEntryAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal(Encoding.UTF8.GetByteCount(Content), entry.Size);
        Assert.Equal(key, entry.Key);
    }

    /// <summary>
    /// Existence is reported for both answers.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Existence_is_reported_for_both_answers()
    {
        // Arrange
        var store = CreateStore();
        var written = CreateKey();
        var missing = CreateKey();
        await WriteAsync(store, written, Content);

        // Act
        // Assert
        Assert.True(await store.ExistsAsync(written, TestContext.Current.CancellationToken));
        Assert.False(await store.ExistsAsync(missing, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Deleting something that is not there is a success.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Deleting_something_that_is_not_there_is_a_success()
    {
        // Arrange
        // The single most important promise of the module: the catalogue and the store are allowed to drift
        var store = CreateStore();

        // Act
        var deleted = await store.DeleteAsync(CreateKey(), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(deleted);
    }

    /// <summary>
    /// Deleting twice is a success both times.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Deleting_twice_is_a_success_both_times()
    {
        // Arrange
        // Outbox delivery is at-least-once, so this is the normal case rather than the strange one
        var store = CreateStore();
        var key = CreateKey();
        await WriteAsync(store, key, Content);

        // Act
        var first = await store.DeleteAsync(key, TestContext.Current.CancellationToken);
        var second = await store.DeleteAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(first);
        Assert.False(second);
        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Deleting one blob leaves its neighbours alone.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Deleting_one_blob_leaves_its_neighbours_alone()
    {
        // Arrange
        var store = CreateStore();
        var deleted = CreateKey();
        var kept = CreateKey();
        await WriteAsync(store, deleted, Content);
        await WriteAsync(store, kept, Content);

        // Act
        await store.DeleteAsync(deleted, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(await store.ExistsAsync(kept, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Inspecting a written blob reports when it was written by the clock of the store.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Inspecting_a_written_blob_reports_when_it_was_written_by_the_clock_of_the_store()
    {
        // Arrange
        // The sweep tells a running upload from an orphan by comparing this time with its own clock
        var store = CreateStore(new FakeTimeProvider(s_now));
        var key = CreateKey();
        var receipt = await WriteAsync(store, key, Content);

        // Act
        var entry = await store.GetEntryAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal(s_now, entry.WrittenAt);
        Assert.Equal(receipt.WrittenAt, entry.WrittenAt);
    }

    /// <summary>
    /// Copying duplicates the content.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Copying_duplicates_the_content()
    {
        // Arrange
        var store = CreateStore();
        var source = CreateKey();
        var destination = CreateKey();
        await WriteAsync(store, source, Content);

        // Act
        var copied = await store.CopyAsync(source, destination, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(copied);
        Assert.Equal(Content, await ReadAsync(store, destination));
        Assert.Equal(Content, await ReadAsync(store, source));
    }

    /// <summary>
    /// Copying refuses a destination that is already taken, and leaves it unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Copying_refuses_a_destination_that_is_already_taken()
    {
        // Arrange
        var store = CreateStore();
        var source = CreateKey();
        var destination = CreateKey();
        await WriteAsync(store, source, Content);
        await WriteAsync(store, destination, "other");

        // Act
        // Assert
        // Cleaning up after the failure must never touch content the copy did not create
        await Assert.ThrowsAsync<BlobException>(() => store.CopyAsync(source, destination, TestContext.Current.CancellationToken));
        Assert.Equal("other", await ReadAsync(store, destination));
    }

    /// <summary>
    /// Copying something that is not there is a success that copied nothing.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the contract has been checked.</returns>
    [Fact]
    public async Task Copying_something_that_is_not_there_is_a_success_that_copied_nothing()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var copied = await store.CopyAsync(CreateKey(), CreateKey(), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(copied);
    }

    /// <summary>
    /// Builds an address nothing else in a test will collide with.
    /// </summary>
    /// <param name="container">The container to address, or null for the default.</param>
    /// <returns>The address.</returns>
    protected static BlobKey CreateKey(BlobContainer? container = null)
        => BlobKey.Create(container ?? BlobContainer.Create("invoices").Value, BlobId.New());

    /// <summary>
    /// Writes text to an address.
    /// </summary>
    /// <param name="store">The store to write to.</param>
    /// <param name="key">The address to write to.</param>
    /// <param name="content">The text to write.</param>
    /// <param name="options">How to write it, or null for the defaults.</param>
    /// <returns>What was written.</returns>
    protected static async Task<BlobWriteReceipt> WriteAsync(IBlobStore store, BlobKey key, string content, BlobWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        using var source = new MemoryStream(Encoding.UTF8.GetBytes(content));

        return await store.WriteAsync(key, source, options ?? BlobWriteOptions.Default, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads an address back as text.
    /// </summary>
    /// <param name="store">The store to read from.</param>
    /// <param name="key">The address to read.</param>
    /// <returns>The text, or null when there is nothing at the address.</returns>
    protected static async Task<string?> ReadAsync(IBlobStore store, BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(store);

        var content = await store.OpenReadAsync(key, TestContext.Current.CancellationToken);
        if (content is null)
        {
            return null;
        }

        await using (content)
        {
            using var reader = new StreamReader(content, Encoding.UTF8);
            return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Creates the store under test on the system clock. Called once per test, so each gets an empty one.
    /// </summary>
    /// <returns>The store to exercise.</returns>
    protected IBlobStore CreateStore() => CreateStore(TimeProvider.System);

    /// <summary>
    /// Creates the store under test. Called once per test, so each gets an empty one.
    /// </summary>
    /// <param name="timeProvider">The clock the store has to stamp content with.</param>
    /// <returns>The store to exercise.</returns>
    protected abstract IBlobStore CreateStore(TimeProvider timeProvider);

    /// <summary>
    /// A stream that can only be read forwards and does not know its own length, like a request body.
    /// </summary>
    /// <param name="content">The content to serve.</param>
    private sealed class ForwardOnlyStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
