namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// Identifies one enqueued message, as it was when it was enqueued or last postponed.
/// </summary>
/// <remarks>
/// Hand it back to <see cref="IOutbox{TContext}.RemoveOnSaveAsync"/>, <see cref="IOutbox{TContext}.PostponeOnSaveAsync"/> or their <c>*NowAsync</c> twins.
/// </remarks>
/// <param name="Id">The identity of the message.</param>
/// <param name="ConcurrencyStamp">The version of the message the ticket was issued for.</param>
public record struct OutboxTicket(Guid Id, Guid ConcurrencyStamp);
