using System.Text.Json;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

// TODO maybe make standalone IValidateAppOptions to remove duplicate validation logic in OutboxOptionsValidation

/// <summary>
/// Behaviour of the outbox engine, configured in code with <see cref="HostApplicationBuilderExtensions.AddOutbox{TDbContext}"/>.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets how many messages a single call to <see cref="IOutboxProcessor.ProcessPendingAsync"/> may deliver.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets how long a lock on a message is honoured.
    /// Once it elapses, another processor may take the message (processor failed).
    /// Set it comfortably above the slowest expected handler.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the backoff schedule for failed deliveries.
    /// </summary>
    /// <remarks>
    /// The list length is the number of retries:
    /// <list type="bullet">
    ///     <item>the first failure waits the first delay,</item>
    ///     <item>second failure the second,</item>
    ///     <item>...</item>
    /// </list>
    ///  the
    /// An empty list dead-letters on the first failure.
    /// </remarks>
    public IReadOnlyList<TimeSpan> RetryDelays { get; set; } =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
    ];

    /// <summary>
    /// Gets or sets an optional upper bound on the number of characters of a serialized payload.
    /// Null, the default, leaves it unbounded.
    /// </summary>
    /// <remarks>
    /// Use it to keep large content out of the message table/storage.
    /// </remarks>
    public int? MaxPayloadLength { get; set; }

    /// <summary>
    /// Gets or sets how long delivered messages are kept before the processor deletes them.
    /// Null, the default, keeps them forever.
    /// </summary>
    /// <remarks>
    /// Only delivered messages are ever deleted. Dead-lettered ones stay until an operator has dealt with them.
    /// </remarks>
    public TimeSpan? Retention { get; set; }

    /// <summary>
    /// Gets or sets the options used to serialize and deserialize payloads.
    /// </summary>
    /// <remarks>
    /// Changing this after rows exist can make stored payloads unreadable, which dead-letters them.
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; set; } = JsonSerializerOptions.Web;
}
