namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// A message enqueued inside a business transaction and delivered afterwards, at least once.
/// </summary>
/// <remarks>
/// <para>
/// Implement this on a plain serializable type - typically a <see langword="record"/>.
/// Pair it with an <see cref="IOutboxMessageHandler{TMessage}"/>.
/// The message is serialized to JSON at enqueue time, so every member must round-trip through <see cref="System.Text.Json.JsonSerializer"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record SendEmailMessage(string Recipient, string Template) : IOutboxMessage
/// {
///     public static OutboxMessageType MessageType =&gt; "email.send.v1";
/// }
/// </code>
/// </example>
public interface IOutboxMessage
{
    /// <summary>
    /// Gets the stable storage key of this message type.
    /// </summary>
    /// <remarks>
    /// It is written to every row and must never change once rows carrying it exist.
    /// End it with a version segment such as <c>.v1</c> - see <see cref="OutboxMessageType"/> for how a changed contract is versioned.
    /// </remarks>
    static abstract OutboxMessageType MessageType { get; }
}
