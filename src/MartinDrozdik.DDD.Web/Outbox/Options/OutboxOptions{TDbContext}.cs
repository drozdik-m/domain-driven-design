using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

// TODO maybe make standalone IValidateAppOptions to remove duplicate validation logic in OutboxOptionsValidation

/// <summary>
/// Behaviour of the outbox over <typeparamref name="TDbContext"/>, configured in code with
/// <see cref="HostApplicationBuilderExtensions.AddOutbox{TDbContext}(IHostApplicationBuilder, Action{OutboxOptions{TDbContext}}, Action{OutboxConfig})"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
#pragma warning disable S2326 // Unused type parameters - used to mark the outbox the options belong to
public sealed class OutboxOptions<TDbContext>
#pragma warning restore S2326
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets or sets how many messages a single call to <see cref="IOutboxProcessor{TDbContext}.ProcessPendingAsync"/> reserves and may deliver.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets how long a lock on a message being delivered is honoured.
    /// Starts when the delivery of that message starts.
    /// Once it elapses, another processor may take the message (processor failed).
    /// Set it comfortably above the slowest expected handler.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets how long a run holds the messages of its batch that it has not started yet.
    /// Once it elapses, the rest of a slow batch goes to other processors, and the run skips those messages.
    /// </summary>
    /// <remarks>
    /// A run reserves its whole batch up front, so processors running together deliver separate batches.
    /// A message being delivered is held by <see cref="LeaseDuration"/> instead. Must not exceed it.
    /// </remarks>
    public TimeSpan ReservationDuration { get; set; } = TimeSpan.FromMinutes(1);

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
    /// An empty list dead-letters on the first failure.
    /// A delay only makes the message due again. Timing also depends on the next run of the consumer (f.e. <see cref="RecurringTasks.Options.RecurringTaskOptions{TTask}.Period"/>).
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
    /// Gets or sets the options used to serialize and deserialize payloads of this outbox.
    /// </summary>
    /// <remarks>
    /// Changing this after rows exist can make stored payloads unreadable, which dead-letters them.
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; set; } = JsonSerializerOptions.Web;
}
