using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Registers the message types for one outbox.
/// </summary>
/// <example>
/// <code>
/// public static OutboxConfig WithEmails(this OutboxConfig config)
///     =&gt; config.WithMessage&lt;SendEmailMessage, SendEmailHandler&gt;();
/// </code>
/// </example>
public sealed class OutboxConfig
{
    private readonly IServiceCollection _services;
    private readonly Action<OutboxRegistration> _register;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxConfig"/> class.
    /// </summary>
    /// <param name="services">Services to register handlers to.</param>
    /// <param name="register">Records a message type in the registry of the outbox being configured.</param>
    internal OutboxConfig(IServiceCollection services, Action<OutboxRegistration> register)
    {
        _services = services;
        _register = register;
    }

    /// <summary>
    /// Registers <typeparamref name="TMessage"/> with this outbox, together with the delivery handler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handler is scoped and resolved once per delivered message.
    /// </para>
    /// <para>
    /// A message type may be registered with several outboxes.
    /// Its handler is then registered once for the whole application, so it is the same handler in every outbox.
    /// </para>
    /// </remarks>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="THandler">The handler delivering the message.</typeparam>
    /// <returns>This for chaining.</returns>
    /// <exception cref="OutboxException">
    /// The message type, or another one using its <see cref="OutboxMessageType"/>, is already registered with this outbox,
    /// or another handler already delivers <typeparamref name="TMessage"/>.
    /// </exception>
    public OutboxConfig WithMessage<TMessage, THandler>()
        where TMessage : IOutboxMessage
        where THandler : class, IOutboxMessageHandler<TMessage>
    {
        _register(OutboxRegistration.For<TMessage>());
        AddHandlerOnce<TMessage, THandler>();

        return this;
    }

    /// <summary>
    /// Registers the handler of <typeparamref name="TMessage"/>, unless an outbox already did.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="THandler">The handler delivering the message.</typeparam>
    /// <exception cref="OutboxException">Another handler already delivers <typeparamref name="TMessage"/>.</exception>
    private void AddHandlerOnce<TMessage, THandler>()
        where TMessage : IOutboxMessage
        where THandler : class, IOutboxMessageHandler<TMessage>
    {
        var existing = _services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == typeof(IOutboxMessageHandler<TMessage>) && !descriptor.IsKeyedService);

        if (existing is null)
        {
            _services.AddScoped<IOutboxMessageHandler<TMessage>, THandler>();
            return;
        }

        if (existing.ImplementationType != typeof(THandler))
        {
            var existingName = existing.ImplementationType?.GetReadableName() ?? "a factory or an instance";
            throw new OutboxException($"Outbox message type '{TMessage.MessageType}' ({typeof(TMessage).GetReadableName()}) is delivered by both {existingName} and {typeof(THandler).GetReadableName()}. A message type has one handler across every outbox of the application.");
        }
    }
}
