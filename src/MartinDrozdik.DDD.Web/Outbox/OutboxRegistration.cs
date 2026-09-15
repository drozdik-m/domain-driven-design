using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// One message type the outbox knows how to deliver.
/// </summary>
/// <param name="MessageType">The storage key written to every row of this type.</param>
/// <param name="MessageClrType">The message type messages of this kind deserialize into.</param>
/// <param name="HandlerServiceType">The closed <see cref="IOutboxMessageHandler{TMessage}"/> that delivers the message. Resolving it is how a test proves the handler can be constructed with its dependencies.</param>
public sealed record OutboxRegistration(
    OutboxMessageType MessageType,
    Type MessageClrType,
    Type HandlerServiceType);
