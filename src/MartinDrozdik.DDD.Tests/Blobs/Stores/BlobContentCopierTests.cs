using System.Security.Cryptography;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs.Stores;

public class BlobContentCopierTests
{
    // Larger than the copier's buffer, so the content is moved in several reads
    private const int MultiBufferSize = 200_000;

    private static readonly DateTimeOffset s_now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CopyAsync_copies_the_content_byte_for_byte()
    {
        // Arrange
        var content = CreateContent(MultiBufferSize);
        using var source = new MemoryStream(content);
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, BlobWriteOptions.Default);

        // Assert
        Assert.Equal(content, destination.ToArray());
        Assert.Equal(content.Length, receipt.Size);
    }

    [Fact]
    public async Task CopyAsync_computes_the_sha256_checksum_of_the_content()
    {
        // Arrange
        var content = CreateContent(MultiBufferSize);
        using var source = new MemoryStream(content);
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, BlobWriteOptions.Default);

        // Assert
        Assert.NotNull(receipt.Checksum);
        Assert.Equal(ChecksumAlgorithm.Sha256, receipt.Checksum.Algorithm);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), receipt.Checksum.Value);
    }

    [Fact]
    public async Task CopyAsync_skips_the_checksum_when_hashing_is_turned_off()
    {
        // Arrange
        var content = CreateContent(1_000);
        using var source = new MemoryStream(content);
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, new BlobWriteOptions(ComputeChecksum: false));

        // Assert
        Assert.Null(receipt.Checksum);
        Assert.Equal(content.Length, receipt.Size);
        Assert.Equal(content, destination.ToArray());
    }

    [Fact]
    public async Task CopyAsync_accepts_empty_content()
    {
        // Arrange
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, BlobWriteOptions.Default);

        // Assert
        Assert.Equal(0, receipt.Size);
        Assert.Equal(0, destination.Length);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData([])), receipt.Checksum?.Value);
    }

    [Fact]
    public async Task CopyAsync_copies_only_what_follows_the_current_position_of_the_source()
    {
        // Arrange
        var content = CreateContent(100);
        using var source = new MemoryStream(content);
        source.Position = 40;
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, BlobWriteOptions.Default);

        // Assert
        Assert.Equal(content[40..], destination.ToArray());
        Assert.Equal(60, receipt.Size);
    }

    [Fact]
    public async Task CopyAsync_accepts_content_exactly_at_the_size_limit()
    {
        // Arrange
        var content = CreateContent(MultiBufferSize);
        using var source = new MemoryStream(content);
        using var destination = new MemoryStream();

        // Act
        var receipt = await CopyAsync(source, destination, new BlobWriteOptions(MaxSize: content.Length));

        // Assert
        Assert.Equal(content.Length, receipt.Size);
        Assert.Equal(content, destination.ToArray());
    }

    [Fact]
    public async Task CopyAsync_throws_for_content_over_the_size_limit()
    {
        // Arrange
        const long maxSize = 100;
        using var source = new MemoryStream(CreateContent(101));
        using var destination = new MemoryStream();

        // Act
        var exception = await Assert.ThrowsAsync<BlobTooLargeException>(
            () => CopyAsync(source, destination, new BlobWriteOptions(MaxSize: maxSize)));

        // Assert
        Assert.Equal(maxSize, exception.MaxSize);
    }

    [Fact]
    public async Task CopyAsync_never_writes_past_the_size_limit()
    {
        // Arrange
        const long maxSize = 100_000;
        using var source = new MemoryStream(CreateContent(MultiBufferSize));
        using var destination = new MemoryStream();

        // Act
        var exception = await Assert.ThrowsAsync<BlobTooLargeException>(
            () => CopyAsync(source, destination, new BlobWriteOptions(MaxSize: maxSize)));

        // Assert
        // The read that crosses the limit is abandoned before it reaches the destination
        Assert.Equal(maxSize, exception.MaxSize);
        Assert.InRange(destination.Length, 0, maxSize);
    }

    [Fact]
    public async Task CopyAsync_stamps_the_receipt_with_the_current_time()
    {
        // Arrange
        var time = new FakeTimeProvider(s_now);
        using var source = new MemoryStream(CreateContent(10));
        using var destination = new MemoryStream();

        // Act
        var receipt = await BlobContentCopier.CopyAsync(source, destination, BlobWriteOptions.Default, time, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(s_now, receipt.WrittenAt);
    }

    [Fact]
    public async Task CopyAsync_flushes_the_destination()
    {
        // Arrange
        using var source = new MemoryStream(CreateContent(10));
        using var destination = new FlushTrackingStream();

        // Act
        await CopyAsync(source, destination, BlobWriteOptions.Default);

        // Assert
        Assert.True(destination.WasFlushed);
    }

    [Fact]
    public async Task CopyAsync_stops_when_cancelled()
    {
        // Arrange
        using var source = new MemoryStream(CreateContent(10));
        using var destination = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => BlobContentCopier.CopyAsync(source, destination, BlobWriteOptions.Default, TimeProvider.System, cancellation.Token));

        // Assert
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public async Task CopyAsync_throws_for_null_arguments()
    {
        // Arrange
        using var stream = new MemoryStream();
        var options = BlobWriteOptions.Default;
        var time = TimeProvider.System;
        var token = TestContext.Current.CancellationToken;

        // Act
        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>("source", () => BlobContentCopier.CopyAsync(null!, stream, options, time, token));
        await Assert.ThrowsAsync<ArgumentNullException>("destination", () => BlobContentCopier.CopyAsync(stream, null!, options, time, token));
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => BlobContentCopier.CopyAsync(stream, stream, null!, time, token));
        await Assert.ThrowsAsync<ArgumentNullException>("timeProvider", () => BlobContentCopier.CopyAsync(stream, stream, options, null!, token));
    }

    private static Task<BlobWriteReceipt> CopyAsync(Stream source, Stream destination, BlobWriteOptions options)
        => BlobContentCopier.CopyAsync(source, destination, options, new FakeTimeProvider(s_now), TestContext.Current.CancellationToken);

    private static byte[] CreateContent(int length)
    {
        var content = new byte[length];
        new Random(length).NextBytes(content);
        return content;
    }

    private sealed class FlushTrackingStream : MemoryStream
    {
        public bool WasFlushed { get; private set; }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            WasFlushed = true;
            return base.FlushAsync(cancellationToken);
        }
    }
}
