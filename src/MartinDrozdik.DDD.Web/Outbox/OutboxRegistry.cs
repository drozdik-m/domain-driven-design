using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// The message types registered with the outbox, captured at startup.
/// Case insensitive - more portable, allows case-insensitive collations etc.
/// </summary>
/// <remarks>
/// It exists so a duplicate <see cref="OutboxMessageType"/> is rejected while the container is being built
/// rather than silently overwriting a dispatcher registration.
/// Also tests and diagnostics can report what the application actually knows how to deliver.
/// </remarks>
public sealed class OutboxRegistry
{
    private readonly Dictionary<string, OutboxRegistration> _registrations = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Gets every registered message type, in registration order.
    /// </summary>
    public IReadOnlyCollection<OutboxRegistration> Registrations => _registrations.Values;

    /// <summary>
    /// Decides whether a message type is registered.
    /// Case insensitive.
    /// </summary>
    /// <param name="messageType">The message type to look for.</param>
    /// <returns>True when a handler is registered for the type, else false.</returns>
    public bool Contains(OutboxMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _registrations.ContainsKey(messageType.Key);
    }

    /// <summary>
    /// Records a message type and the handler that delivers it.
    /// Case insensitive.
    /// </summary>
    /// <param name="registration">The registration to record.</param>
    /// <exception cref="OutboxException">The message type is already registered.</exception>
    internal void Add(OutboxRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (_registrations.TryGetValue(registration.MessageType.Key, out var existing))
        {
            throw new OutboxException($"Outbox message type '{registration.MessageType}' is registered by both {existing.MessageClrType.GetReadableName()} and {registration.MessageClrType.GetReadableName()}. The key is what maps a stored row back to a type, so it must identify exactly one of them.");
        }

        _registrations.Add(registration.MessageType.Key, registration);
    }
}
