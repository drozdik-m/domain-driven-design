using System.Text.Json;
using System.Transactions;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Enqueues and manages messages in <typeparamref name="TDbContext"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
/// <param name="context">The caller's context, which the message is added to.</param>
/// <param name="scopeFactory">Creates the scope a unit of work of its own is resolved from.</param>
/// <param name="registry">The registered message types, used to reject unhandled messages early.</param>
/// <param name="options">Options carrying the serializer configuration and the payload limit.</param>
/// <param name="timeProvider">Source of the enqueue time.</param>
internal sealed class Outbox<TDbContext>(
    TDbContext context,
    IServiceScopeFactory scopeFactory,
    OutboxRegistry registry,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider) : IOutbox<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public OutboxTicket AddOnSave<TMessage>(TMessage message, OutboxMessageSettings? settings = null)
        where TMessage : IOutboxMessage
    {
        var outboxMessage = CreateOutboxMessage(message, settings ?? OutboxMessageSettings.Default);

        // Only tracked - the caller's own SaveChanges commits the message together with whatever produced it.
        context.Set<OutboxMessage>().Add(outboxMessage);

        return outboxMessage.ToTicket();
    }

    /// <inheritdoc />
    public async Task<OutboxTicket> AddNowAsync<TMessage>(TMessage message, OutboxMessageSettings? settings, CancellationToken cancellationToken)
        where TMessage : IOutboxMessage
    {
        var outboxMessage = CreateOutboxMessage(message, settings ?? OutboxMessageSettings.Default);

        // Committed on its own right now
        await InOwnUnitOfWorkAsync(async ownContext =>
        {
            ownContext.Set<OutboxMessage>().Add(outboxMessage);
            await ownContext.SaveChangesAsync(cancellationToken);
        });

        return outboxMessage.ToTicket();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveOnSaveAsync(OutboxTicket ticket, CancellationToken cancellationToken)
    {
        // Try to find the message
        var entry = await FindAsync(ticket, cancellationToken);
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
    public async Task<bool> RemoveNowAsync(OutboxTicket ticket, CancellationToken cancellationToken)
    {
        // Conditional on the stamp of the ticket to prevent race conditions
        var deleted = 0;
        await InOwnUnitOfWorkAsync(async ownContext =>
        {
            deleted = await ownContext.Set<OutboxMessage>()
                        .Where(m => m.Id == ticket.Id && m.ConcurrencyStamp == ticket.ConcurrencyStamp)
                        .ExecuteDeleteAsync(cancellationToken);
        });

        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<OutboxTicket?> PostponeOnSaveAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken)
    {
        // Try to find the message
        var entry = await FindAsync(ticket, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        // Postpone it in the caller's context
        entry.Entity.Postpone(availableAt.UtcDateTime);
        return entry.Entity.ToTicket();
    }

    /// <inheritdoc />
    public async Task<OutboxTicket?> PostponeNowAsync(OutboxTicket ticket, DateTimeOffset availableAt, CancellationToken cancellationToken)
    {
        var availableAtUtc = availableAt.UtcDateTime;
        var rotatedConcurrencyStamp = Guid.CreateVersion7();

        // Conditional on the stamp of the ticket to prevent race conditions
        var updated = 0;
        await InOwnUnitOfWorkAsync(async ownContext =>
        {
            updated = await ownContext.Set<OutboxMessage>()
                        .Where(e => e.Id == ticket.Id && e.ConcurrencyStamp == ticket.ConcurrencyStamp)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(m => m.AvailableAt, availableAtUtc)
                                .SetProperty(m => m.ConcurrencyStamp, rotatedConcurrencyStamp),
                            cancellationToken);
        });

        return updated > 0
            ? ticket with { ConcurrencyStamp = rotatedConcurrencyStamp }
            : null;
    }

    /// <summary>
    /// Finds the version of a message a ticket saw, tracked in the caller's context.
    /// </summary>
    /// <remarks>
    /// A message already tracked is used without a query.
    /// A tracked copy older than the ticket is reloaded first.
    /// </remarks>
    /// <param name="ticket">The ticket of the message.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The entry of the message, or null when it is gone, being removed, or changed since the ticket was issued.</returns>
    private async Task<EntityEntry<OutboxMessage>?> FindAsync(OutboxTicket ticket, CancellationToken cancellationToken)
    {
        // The message is not tracked, so query it.
        var message = await context.Set<OutboxMessage>().FindAsync([ticket.Id], cancellationToken);
        if (message is null)
        {
            return null;
        }

        // The message is tracked, but the ticket is newer than the tracked copy. Reload it to see if it is still there.
        var entry = context.Entry(message);
        if (entry.State == EntityState.Unchanged && message.ConcurrencyStamp != ticket.ConcurrencyStamp)
        {
            await entry.ReloadAsync(cancellationToken);
        }

        // The message is tracked and the ticket is current. Return it, or null if it was removed.
        var isCurrent = entry.State != EntityState.Detached
            && entry.State != EntityState.Deleted
            && message.ConcurrencyStamp == ticket.ConcurrencyStamp;

        return isCurrent ? entry : null;
    }

    /// <summary>
    /// Checks and serializes a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="settings">How the message is enqueued.</param>
    /// <returns>The message, ready to be stored.</returns>
    /// <exception cref="OutboxException">The message type has no registered handler, or the message cannot be stored.</exception>
    private OutboxMessage CreateOutboxMessage<TMessage>(TMessage message, OutboxMessageSettings settings)
        where TMessage : IOutboxMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = TMessage.MessageType;
        if (!registry.Contains(messageType))
        {
            throw new OutboxException($"Outbox message type '{messageType}' has no registered handler. Register it with WithMessage<{typeof(TMessage).GetReadableName()}, ...>() when calling AddOutbox.");
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

    /// <summary>
    /// Runs work on a fresh context, outside every transaction of the caller.
    /// </summary>
    /// <remarks>
    /// A new scope gives a new instance of <typeparamref name="TDbContext"/> with a connection of its own.
    /// Suppressing the ambient <see cref="TransactionScope"/> keeps that connection from enlisting in the caller's one.
    /// </remarks>
    /// <param name="work">The work to do.</param>
    /// <returns>A task completing once the work is committed.</returns>
    private async Task InOwnUnitOfWorkAsync(Func<TDbContext, Task> work)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var ownContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        using var suppressed = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await work(ownContext);
        suppressed.Complete();
    }
}
