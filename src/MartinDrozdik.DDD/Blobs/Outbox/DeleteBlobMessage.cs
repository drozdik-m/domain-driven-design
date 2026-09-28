using System.Text.Json;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Blobs.Outbox;

/// <summary>
/// Removes the content of one blob.
/// </summary>
/// <remarks>
/// The address travels as plain strings rather than value objects due to <see cref="JsonSerializer"/>,
/// and by the time the message is delivered the original <see cref="Blob"/> record no longer exists.
/// <para>
/// Idempotent - delivering it twice is harmless.
/// </para>
/// </remarks>
/// <param name="Container">The container the blob lived in.</param>
/// <param name="BlobId">The identity of the blob.</param>
public sealed record DeleteBlobMessage(string Container, string BlobId) : IOutboxMessage
{
    /// <summary>
    /// Gets the stable storage key of this message type.
    /// </summary>
    public static OutboxMessageType MessageType => "blob.delete.v1";

    /// <summary>
    /// Builds the message that removes the content at an address.
    /// </summary>
    /// <param name="key">The address to remove.</param>
    /// <returns>The message.</returns>
    public static DeleteBlobMessage For(BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return new(key.Container.Name, key.Id.ToString());
    }
}
