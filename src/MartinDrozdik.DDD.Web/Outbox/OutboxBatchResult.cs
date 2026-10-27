namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// The outcome of one call to <see cref="IOutboxProcessor{TDbContext}.ProcessPendingAsync"/>.
/// </summary>
/// <param name="Found">How many free messages the batch read, before reserving them.</param>
/// <param name="Reserved">How many of them the batch reserved. Another processor may have reserved the rest first.</param>
/// <param name="Released">How many reserved messages the batch released untouched: not attempted because the run was cancelled, their reservation had expired, or they could not be delivered here.</param>
/// <param name="Dispatched">
/// How many messages were delivered successfully.
/// Messages that were retried, dead-lettered, skipped or claimed by another processor are not counted.
/// </param>
public sealed record OutboxBatchResult(int Found, int Reserved, int Released, int Dispatched)
{
    /// <summary>
    /// Gets a value indicating whether the batch found messages and moved at least one of them on, so more may be waiting.
    /// Call again while it is true to drain a backlog.
    /// </summary>
    /// <remarks>
    /// False once a batch finds nothing.
    /// Also false when the batch released every message it found untouched: calling again would find the same messages and spin.
    /// A message another processor reserved first counts as moved on.
    /// </remarks>
    public bool MayHaveMore => Found > 0 && Released < Found;
}
