namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// Delivers/resolves one type of stored outbox message.
/// </summary>
/// <remarks>
/// Resolved from a fresh dependency injection scope for every message, so scoped services may be injected through the constructor.
/// Report failure by throwing. The processor records the error, waits the next configured retry delay and tries again until it succeeds or is dead-lettered.
/// </remarks>
/// <typeparam name="TMessage">The message this handler delivers.</typeparam>
public interface IOutboxMessageHandler<in TMessage>
    where TMessage : IOutboxMessage
{
    /// <summary>
    /// Delivers and resolves a single message.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the message has been delivered.</returns>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
