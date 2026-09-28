using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Blobs.Stores;
using Microsoft.Extensions.Logging.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// What the file store leaves behind when a write does not finish.
/// </summary>
/// <remarks>
/// Writes go straight to their final name, so a failed one has to be cleaned up on the spot. Nothing reads it in
/// the meantime - no catalogue row points at it - and anything the cleanup misses is removed by the outbox
/// message the upload scheduled before writing.
/// </remarks>
public sealed class FileBlobStoreCrashSafetyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_blobs");

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A temp folder is not worth failing a run over
        }
    }

    [Fact]
    public async Task A_write_that_fails_partway_leaves_nothing_under_the_final_name()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();
        await using var content = new FailingStream(failAfter: 8);

        // Act
        await Assert.ThrowsAsync<IOException>(() => store.WriteAsync(key, content, BlobWriteOptions.Default, TestContext.Current.CancellationToken));

        // Assert
        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_write_that_exceeds_the_size_limit_leaves_nothing_behind()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();
        await using var content = new MemoryStream(new byte[1024]);

        // Act
        await Assert.ThrowsAsync<MartinDrozdik.DDD.Blobs.Exceptions.BlobTooLargeException>(
            () => store.WriteAsync(key, content, new BlobWriteOptions(MaxSize: 16), TestContext.Current.CancellationToken));

        // Assert
        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_write_onto_a_taken_address_leaves_the_existing_content_alone()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();
        await using var original = new MemoryStream([1, 2, 3]);
        await store.WriteAsync(key, original, BlobWriteOptions.Default, TestContext.Current.CancellationToken);
        await using var intruder = new MemoryStream([4, 5, 6, 7]);

        // Act
        await Assert.ThrowsAsync<MartinDrozdik.DDD.Blobs.Exceptions.BlobException>(
            () => store.WriteAsync(key, intruder, BlobWriteOptions.Default, TestContext.Current.CancellationToken));

        // Assert
        var entry = await store.GetEntryAsync(key, TestContext.Current.CancellationToken);
        Assert.NotNull(entry);
        Assert.Equal(3, entry.Size);
    }

    /// <summary>
    /// Builds a fresh address.
    /// </summary>
    /// <returns>The address.</returns>
    private static BlobKey CreateKey() => BlobKey.Create("invoices", BlobId.New());

    /// <summary>
    /// Builds a store over a folder of this test's own.
    /// </summary>
    /// <returns>The store.</returns>
    private IBlobStore CreateStore()
    {
        Directory.CreateDirectory(_root);

        var fileOptions = new FileBlobOptions();
        fileOptions.Containers.Add("invoices", new FileBlobContainerOptions { Path = Path.Combine(_root, "invoices") });
        var options = Microsoft.Extensions.Options.Options.Create(fileOptions);

        return new FileBlobStore(options, TimeProvider.System, NullLogger<FileBlobStore>.Instance);
    }

    /// <summary>
    /// A stream that gives up partway through, the way a dropped connection does.
    /// </summary>
    /// <param name="failAfter">How many bytes to serve before failing.</param>
    private sealed class FailingStream(int failAfter) : Stream
    {
        private int _served;

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
        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            return Read(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            if (_served >= failAfter)
            {
                throw new IOException("The connection went away.");
            }

            var served = Math.Min(buffer.Length, failAfter - _served);
            buffer[..served].Fill(0x42);
            _served += served;

            return served;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
