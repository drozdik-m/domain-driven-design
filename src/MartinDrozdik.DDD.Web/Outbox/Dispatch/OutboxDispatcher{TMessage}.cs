using System.Text.Json;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox.Dispatch;

/// <summary>
/// Dispatches (resolves) one message type.
/// </summary>
/// <remarks>
/// The message type is not statically known yet.
/// One implementation is registered per message type, keyed by <see cref="OutboxMessageType.Key"/>,
/// so the processor recovers the type by resolving a keyed service rather than by reflecting over the stored key.
/// Deserialization and handler resolution then happen inside a closed generic, with no runtime type construction at all.
/// </remarks>
/// <typeparam name="TMessage">The message this dispatcher delivers.</typeparam>
/// <param name="provider">The scope the handler is resolved from.</param>
/// <param name="options">Options carrying the serializer configuration.</param>
internal sealed class OutboxDispatcher<TMessage>(IServiceProvider provider, IOptions<OutboxOptions> options)
    : IOutboxDispatcher
    where TMessage : IOutboxMessage
{
    /// <inheritdoc />
    public Task DispatchAsync(OutboxPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var message = Deserialize(payload);
        var handler = provider.GetRequiredService<IOutboxMessageHandler<TMessage>>();

        return handler.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Reads a stored payload back into a message.
    /// </summary>
    /// <param name="payload">The stored payload.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="OutboxException">The payload cannot be read as <typeparamref name="TMessage"/>.</exception>
    private TMessage Deserialize(OutboxPayload payload)
    {
        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(payload.Value, options.Value.SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new OutboxException($"The stored payload of outbox message type '{TMessage.MessageType}' could not be deserialized into {typeof(TMessage).GetReadableName()}.", exception);
        }

        return message ?? throw new OutboxException($"The stored payload of outbox message type '{TMessage.MessageType}' deserialized to null.");
    }
}
