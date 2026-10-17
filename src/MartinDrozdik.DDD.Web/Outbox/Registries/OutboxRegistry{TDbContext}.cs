using System.Diagnostics.CodeAnalysis;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Outbox.Registries;

/// <summary>
/// The message types registered with the outbox of <typeparamref name="TDbContext"/>, captured at startup.
/// Case insensitive - more portable, allows case-insensitive collations etc.
/// </summary>
/// <remarks>
/// <para>
/// A duplicate <see cref="OutboxMessageType"/> is rejected while the container is being built.
/// Good for tests and diagnostics.
/// </para>
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
public sealed class OutboxRegistry<TDbContext>
    where TDbContext : DbContext
{
    private static readonly string s_outboxName = typeof(TDbContext).GetReadableName();

    private readonly Dictionary<string, OutboxRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRegistry{TDbContext}"/> class.
    /// </summary>
    internal OutboxRegistry()
    {
    }

    /// <summary>
    /// Gets every registered message type, in registration order.
    /// </summary>
    public IReadOnlyCollection<OutboxRegistration> Registrations => _registrations.Values;

    /// <summary>
    /// Decides whether a message type is registered.
    /// Case insensitive.
    /// </summary>
    /// <param name="messageType">The message type to look for.</param>
    /// <returns>True when the type is registered with this outbox, else false.</returns>
    public bool Contains(OutboxMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _registrations.ContainsKey(messageType.Key);
    }

    /// <summary>
    /// Finds the registration of a message type.
    /// Case insensitive.
    /// </summary>
    /// <param name="messageType">The message type to look for, e.g. as read from a stored row.</param>
    /// <param name="registration">The registration, carrying the key exactly as it was registered.</param>
    /// <returns>True when the type is registered with this outbox, else false.</returns>
    public bool TryGet(OutboxMessageType messageType, [NotNullWhen(true)] out OutboxRegistration? registration)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _registrations.TryGetValue(messageType.Key, out registration);
    }

    /// <summary>
    /// Finds the registry of the outbox over <typeparamref name="TDbContext"/> while the container is still being built.
    /// </summary>
    /// <param name="services">The services the outbox may have been added to.</param>
    /// <param name="registry">The registry, when the outbox has been added.</param>
    /// <returns>True when the outbox over <typeparamref name="TDbContext"/> has been added, else false.</returns>
    internal static bool TryFind(IServiceCollection services, [NotNullWhen(true)] out OutboxRegistry<TDbContext>? registry)
    {
        ArgumentNullException.ThrowIfNull(services);

        registry = services
            .Where(descriptor => descriptor.ServiceType == typeof(OutboxRegistry<TDbContext>) && !descriptor.IsKeyedService)
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<OutboxRegistry<TDbContext>>()
            .FirstOrDefault();

        return registry is not null;
    }

    /// <summary>
    /// Records a message type and the dispatch code that delivers it to its handler.
    /// Case insensitive.
    /// </summary>
    /// <param name="registration">The registration to record.</param>
    /// <exception cref="OutboxException">The message type is already registered with this outbox.</exception>
    internal void Add(OutboxRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (_registrations.TryGetValue(registration.MessageType.Key, out var existing))
        {
            throw existing.MessageClrType == registration.MessageClrType
                ? new OutboxException($"Outbox message type '{registration.MessageType}' ({registration.MessageClrType.GetReadableName()}) is registered twice with the outbox of {s_outboxName}. Register it once.")
                : new OutboxException($"Outbox message type '{registration.MessageType}' is registered with the outbox of {s_outboxName} by both {existing.MessageClrType.GetReadableName()} and {registration.MessageClrType.GetReadableName()}. The key is what maps a stored row back to a type, so it must identify exactly one of them.");
        }

        _registrations.Add(registration.MessageType.Key, registration);
    }
}
