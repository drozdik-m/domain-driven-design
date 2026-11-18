using System.Text.Json;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Enqueues and manages messages.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="registry">The message types of this outbox, used to reject unregistered messages early.</param>
/// <param name="options">Options carrying the serializer configuration and the payload limit.</param>
/// <param name="timeProvider">Source of the enqueue time.</param>
internal sealed class Outbox<TDbContext>(
    OutboxRegistry<TDbContext> registry,
    IOptions<OutboxOptions<TDbContext>> options,
    TimeProvider timeProvider) : IOutbox<TDbContext>
    where TDbContext : DbContext
{
    private static readonly string s_outboxName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public OutboxTicket AddOnSave<TMessage>(TDbContext context, TMessage message)
        where TMessage : IOutboxMessage
        => AddOnSave(context, message, OutboxMessageSettings.Default);

    /// <inheritdoc />
    public OutboxTicket AddOnSave<TMessage>(TDbContext context, TMessage message, OutboxMessageSettings settings)
        where TMessage : IOutboxMessage
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        var outboxMessage = CreateOutboxMessage(message, settings);

        // Only tracked - the caller's own SaveChanges commits the message together with whatever produced it.
        context.Set<OutboxMessage>().Add(outboxMessage);

        return outboxMessage.ToTicket();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveOnSaveAsync(TDbContext context, OutboxTicket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Try to find the message
        var entry = await FindAsync(context, ticket, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        // Update the state in the caller's context
        entry.State = entry.State == EntityState.Added
            ? EntityState.Detached
            : EntityState.Deleted;
        return true;
    }

    /// <inheritdoc />
    public async Task<OutboxTicket?> PostponeOnSaveAsync(TDbContext context, OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Try to find the message
        // A processed or dead-lettered message is never delivered again, so there is nothing to postpone
        var entry = await FindAsync(context, ticket, cancellationToken);
        if (entry is null || !entry.Entity.IsPending)
        {
            return null;
        }

        // Postpone it in the caller's context
        entry.Entity.Postpone(availableAt.UtcDateTime);
        return entry.Entity.ToTicket();
    }

    /// <summary>
    /// Finds the version of a message a ticket saw, tracked in the caller's context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A message the caller has not saved yet, or has already changed, is used as tracked: its state is the caller's, not the database's.
    /// </para>
    /// <para>
    /// A tracked copy without changes is reloaded first: a processor may have claimed, delivered or removed the row since.
    /// </para>
    /// </remarks>
    /// <param name="context">The caller's context.</param>
    /// <param name="ticket">The ticket of the message.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The entry of the message, or null when it is gone, being removed, or changed since the ticket was issued.</returns>
    private static async Task<EntityEntry<OutboxMessage>?> FindAsync(TDbContext context, OutboxTicket ticket, CancellationToken cancellationToken)
    {
        var entry = context.Set<OutboxMessage>().Local.FindEntry(ticket.Id);
        if (entry is null)
        {
            // Not tracked yet, so load the row
            var message = await context.Set<OutboxMessage>()
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == ticket.Id, cancellationToken);
            if (message is null)
            {
                return null;
            }

            entry = context.Entry(message);
        }
        else if (entry.State == EntityState.Unchanged)
        {
            // The tracked copy may be stale, so reload it
            await entry.ReloadAsync(cancellationToken);
        }

        // Current when the message is still there, not being removed, and has the correct stamp
        var isCurrent = (entry.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged)
            && entry.Entity.ConcurrencyStamp == ticket.ConcurrencyStamp;

        return isCurrent ? entry : null;
    }

    /// <summary>
    /// Checks and serializes a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="settings">How the message is enqueued.</param>
    /// <returns>The message, ready to be stored.</returns>
    /// <exception cref="OutboxException">The message type is not registered with this outbox, its key is registered by another type, or the message cannot be stored.</exception>
    private OutboxMessage CreateOutboxMessage<TMessage>(TMessage message, OutboxMessageSettings settings)
        where TMessage : IOutboxMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = TMessage.MessageType;
        if (!registry.TryGet(messageType, out var registration))
        {
            throw new OutboxException($"Outbox message type '{messageType}' is not registered with the outbox of {s_outboxName}. Register it with WithMessage<{typeof(TMessage).GetReadableName()}, ...>() when calling AddOutbox<{s_outboxName}>.");
        }

        // Validate the actual payload type against the registered type
        if (registration.MessageClrType != typeof(TMessage))
        {
            throw new OutboxException($"Outbox message type '{messageType}' is registered with the outbox of {s_outboxName} by {registration.MessageClrType.GetReadableName()}, not by {typeof(TMessage).GetReadableName()}. Give {typeof(TMessage).GetReadableName()} a key of its own and register it with WithMessage<{typeof(TMessage).GetReadableName()}, ...>().");
        }

        var payload = SerializeMessage(message, messageType);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return OutboxMessage.Create(messageType, payload, occurredAtUtc: now, settings.AvailableAt?.UtcDateTime);
    }

    /// <summary>
    /// Serializes a message and enforces the configured payload limit.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <param name="messageType">The storage key of the message type, reported in errors.</param>
    /// <returns>The serialized payload.</returns>
    /// <exception cref="OutboxException">The message cannot be serialized, or its payload is too long.</exception>
    private OutboxPayload SerializeMessage<TMessage>(TMessage message, OutboxMessageType messageType)
        where TMessage : IOutboxMessage
    {
        string value;
        try
        {
            value = JsonSerializer.Serialize(message, options.Value.SerializerOptions);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            // NotSupportedException: System.Text.Json cannot serialize a type that does not have a public parameterless constructor, or a type that is not supported (e.g. System.Type)
            // JsonException: a reference cycle or the maximum depth exceeded
            // InvalidOperationException: the contract of the type is invalid, e.g. two members share a JSON name under the naming policy
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
