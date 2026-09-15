using System.Text.Json;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Enqueues messages into <typeparamref name="TDbContext"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="context">The context the message is added to.</param>
/// <param name="registry">The registered message types, used to reject unhandled messages early.</param>
/// <param name="options">Options carrying the serializer configuration and the payload limit.</param>
/// <param name="timeProvider">Source of the enqueue time.</param>
internal sealed class Outbox<TDbContext>(
    TDbContext context,
    OutboxRegistry registry,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider) : IOutbox
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public void Add<TMessage>(TMessage message)
        where TMessage : IOutboxMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = TMessage.MessageType;
        if (!registry.Contains(messageType))
        {
            throw new OutboxException($"Outbox message type '{messageType}' has no registered handler. Register it with WithMessage<{typeof(TMessage).Name}, ...>() when calling AddOutbox.");
        }

        var payload = Serialize(message, messageType);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Only tracked - the caller's own SaveChanges commits the message together with whatever produced it.
        context.Set<OutboxMessage>().Add(OutboxMessage.Create(messageType, payload, now));
    }

    /// <summary>
    /// Serializes a message and enforces the configured payload limit.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <param name="messageType">The storage key of the message type, reported in errors.</param>
    /// <returns>The serialized payload.</returns>
    /// <exception cref="OutboxException">The message cannot be serialized, or its payload is too long.</exception>
    private OutboxPayload Serialize<TMessage>(TMessage message, OutboxMessageType messageType)
        where TMessage : IOutboxMessage
    {
        string value;
        try
        {
            value = JsonSerializer.Serialize(message, options.Value.SerializerOptions);
        }
        catch (NotSupportedException exception)
        {
            throw new OutboxException($"Outbox message '{messageType}' could not be serialized. Every member must round-trip through System.Text.Json.", exception);
        }

        var maxLength = options.Value.MaxPayloadLength;
        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new OutboxException($"The payload of outbox message '{messageType}' is {value.Length} characters, but the configured limit is {maxLength.Value}. Store large content elsewhere and reference it from the message.");
        }

        return new OutboxPayload(value);
    }
}
