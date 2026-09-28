using MartinDrozdik.DDD.Blobs.Models;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// An open blob, together with the row describing it.
/// </summary>
/// <remarks>
/// Disposing this closes the stream <see cref="Content"/>.
/// </remarks>
/// <param name="Blob">The catalogue row of the blob.</param>
/// <param name="Content">The content, which the caller owns.</param>
public sealed record BlobContent(Blob Blob, Stream Content) : IAsyncDisposable
{
    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
