using System.Text.Json;
using MartinDrozdik.DDD.Web.Outbox.Models;

namespace MartinDrozdik.DDD.Web.Outbox.Dispatch;

/// <summary>
/// Turns a stored payload back into a typed message and hands it to its handler.
/// </summary>
/// <remarks>
/// One per message type.
/// </remarks>
/// <param name="services">The scope the handler is resolved from.</param>
/// <param name="payload">The stored payload.</param>
/// <param name="serializerOptions">The serializer options of the outbox the payload was read from.</param>
/// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
/// <returns>A <see cref="Task"/> that completes when the handler has finished.</returns>
internal delegate Task OutboxDispatch(
    IServiceProvider services,
    OutboxPayload payload,
    JsonSerializerOptions serializerOptions,
    CancellationToken cancellationToken);
