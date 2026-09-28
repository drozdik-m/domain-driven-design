using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Web.Blobs.Outbox;

/// <summary>
/// Removes the content of a blob.
/// </summary>
/// <param name="store">The store holding the content.</param>
internal sealed class DeleteBlobMessageHandler(IBlobStore store)
    : IOutboxMessageHandler<DeleteBlobMessage>
{
    /// <inheritdoc />
    public Task HandleAsync(DeleteBlobMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var key = BlobKey.Create(
            BlobContainer.Create(message.Container).Value,
            new BlobId(Guid.Parse(message.BlobId)));

        return store.DeleteAsync(key, cancellationToken);
    }
}
