using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Dispatch;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Registers the message types the outbox knows how to deliver.
/// </summary>
/// <param name="services">Services to register handlers and dispatchers to.</param>
/// <param name="registry">The registry recording every registered message type.</param>
public sealed class OutboxConfig(IServiceCollection services, OutboxRegistry registry)
{
    /// <summary>
    /// Registers the handler that delivers <typeparamref name="TMessage"/>.
    /// </summary>
    /// <remarks>
    /// Both the handler and its dispatcher are scoped and resolved once per delivered message.
    /// </remarks>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="THandler">The handler delivering the message.</typeparam>
    /// <returns>This for chaining.</returns>
    /// <exception cref="OutboxException">Another message type is already registered under the same <see cref="OutboxMessageType"/>.</exception>
    public OutboxConfig WithMessage<TMessage, THandler>()
        where TMessage : IOutboxMessage
        where THandler : class, IOutboxMessageHandler<TMessage>
    {
        var outboxRegistration = new OutboxRegistration(TMessage.MessageType, typeof(TMessage), typeof(IOutboxMessageHandler<TMessage>));
        registry.Add(outboxRegistration);

        services.AddScoped<IOutboxMessageHandler<TMessage>, THandler>();
        services.AddKeyedScoped<IOutboxDispatcher, OutboxDispatcher<TMessage>>(TMessage.MessageType.Key);

        return this;
    }
}
