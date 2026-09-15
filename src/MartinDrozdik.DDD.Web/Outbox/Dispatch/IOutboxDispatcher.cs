using MartinDrozdik.DDD.Web.Outbox.Models;

namespace MartinDrozdik.DDD.Web.Outbox.Dispatch;

/// <summary>
/// Turns a stored payload back into a typed message and hands it to its handler.
/// </summary>
internal interface IOutboxDispatcher
{
    /// <summary>
    /// Deserializes a payload and delivers it.
    /// </summary>
    /// <param name="payload">The stored payload.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the handler has finished.</returns>
    Task DispatchAsync(OutboxPayload payload, CancellationToken cancellationToken);
}
