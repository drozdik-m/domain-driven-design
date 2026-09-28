namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// How an outbox message is enqueued.
/// </summary>
public sealed record OutboxMessageSettings
{
    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static OutboxMessageSettings Default { get; } = new() { AvailableAt = null };

    /// <summary>
    /// Gets the earliest instant the message may be delivered, or null for as soon as it is stored.
    /// </summary>
    public required DateTimeOffset? AvailableAt { get; init; }
}
