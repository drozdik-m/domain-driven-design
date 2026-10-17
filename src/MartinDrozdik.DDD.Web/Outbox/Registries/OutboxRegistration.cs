using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Dispatch;

namespace MartinDrozdik.DDD.Web.Outbox.Registries;

/// <summary>
/// One message type the outbox knows how to deliver.
/// </summary>
public sealed class OutboxRegistration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRegistration"/> class.
    /// </summary>
    /// <param name="messageType">The storage key written to every row of this type.</param>
    /// <param name="messageClrType">The type messages of this kind deserialize into.</param>
    /// <param name="dispatch">Delivers a stored payload of this type.</param>
    private OutboxRegistration(OutboxMessageType messageType, Type messageClrType, OutboxDispatch dispatch)
    {
        MessageType = messageType;
        MessageClrType = messageClrType;
        Dispatch = dispatch;
    }

    /// <summary>
    /// Gets the storage key written to every row of this type.
    /// </summary>
    public OutboxMessageType MessageType { get; }

    /// <summary>
    /// Gets the type messages of this kind deserialize into.
    /// Its <see cref="IOutboxMessageHandler{TMessage}"/> is what delivers them.
    /// </summary>
    public Type MessageClrType { get; }

    /// <summary>
    /// Gets the dispatch of this message type, closed over it when it was registered.
    /// </summary>
    internal OutboxDispatch Dispatch { get; }

    /// <summary>
    /// Creates the registration of <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <returns>The registration.</returns>
    internal static OutboxRegistration For<TMessage>()
        where TMessage : IOutboxMessage
        => new(TMessage.MessageType, typeof(TMessage), OutboxDispatcher<TMessage>.DispatchAsync);
}
