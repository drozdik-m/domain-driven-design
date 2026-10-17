using System.Text.Json;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Outbox.Dispatch;

/// <summary>
/// Dispatches (resolves) one message type.
/// </summary>
/// <remarks>
/// The message type is not statically known when a row is read.
/// <see cref="DispatchAsync"/> is captured as the <see cref="OutboxDispatch"/> of the <see cref="OutboxRegistration"/> of <typeparamref name="TMessage"/>,
/// so the processor recovers the type from its registry rather than by reflection.
/// Deserialization and handler resolution then happen inside a closed generic, with no runtime type construction at all.
/// </remarks>
/// <typeparam name="TMessage">The message this dispatcher delivers.</typeparam>
internal static class OutboxDispatcher<TMessage>
    where TMessage : IOutboxMessage
{
    /// <summary>
    /// Deserializes a payload and hands it to the handler of <typeparamref name="TMessage"/>.
    /// </summary>
    /// <param name="services">The scope the handler is resolved from.</param>
    /// <param name="payload">The stored payload.</param>
    /// <param name="serializerOptions">The serializer options of the outbox the payload was read from.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the handler has finished.</returns>
    /// <exception cref="OutboxException">The payload cannot be read as <typeparamref name="TMessage"/>.</exception>
    public static Task DispatchAsync(IServiceProvider services, OutboxPayload payload, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(serializerOptions);

        var message = Deserialize(payload, serializerOptions);
        var handler = services.GetRequiredService<IOutboxMessageHandler<TMessage>>();

        return handler.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Reads a stored payload back into a message.
    /// </summary>
    /// <param name="payload">The stored payload.</param>
    /// <param name="serializerOptions">The serializer options of the outbox the payload was read from.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="OutboxException">The payload cannot be read as <typeparamref name="TMessage"/>.</exception>
    private static TMessage Deserialize(OutboxPayload payload, JsonSerializerOptions serializerOptions)
    {
        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(payload.Value, serializerOptions);
        }
        catch (JsonException exception)
        {
            throw new OutboxException($"The stored payload of outbox message type '{TMessage.MessageType}' could not be deserialized into {typeof(TMessage).GetReadableName()}.", exception);
        }

        return message ?? throw new OutboxException($"The stored payload of outbox message type '{TMessage.MessageType}' deserialized to null.");
    }
}
