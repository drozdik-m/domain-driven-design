using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
    /// Gets the longest <see cref="LeaseDuration"/>, <see cref="ReservationDuration"/> and retry delay in <see cref="RetryDelays"/> the outbox accepts: 365 days.
    /// </summary>
    /// <remarks>
    /// To never retry, leave <see cref="RetryDelays"/> empty.
    /// </remarks>
    public static TimeSpan MaxDuration => TimeSpan.FromDays(365);

    /// <summary>
    /// Gets or sets how many messages a single call to <see cref="IOutboxProcessor{TDbContext}.ProcessPendingAsync"/> reserves and may deliver.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets how long a lock on a message being delivered is honoured.
    /// Starts when the delivery of that message starts.
    /// Once it elapses, another processor may take the message (processor failed).
    /// Set it comfortably above the slowest expected handler.
    /// Must be greater than zero and not longer than <see cref="MaxDuration"/>.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets how long a run holds the messages of its batch that it has not started yet.
    /// Once it elapses, the rest of a slow batch goes to other processors, and the run skips those messages.
    /// </summary>
    /// <remarks>
    /// A run reserves its whole batch up front, so processors running together deliver separate batches.
    /// A message being delivered is held by <see cref="LeaseDuration"/> instead. Must be greater than zero and not exceed it.
    /// </remarks>
    public TimeSpan ReservationDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets how far the clocks of two processors may differ.
    /// </summary>
    /// <remarks>
    /// Clocks may differ between consumers.
    /// This ensures reasonable time tolerance for leases.
    /// Keep it above the worst skew between your instances.
    /// Can be zero for single instance.
    /// </remarks>
    public TimeSpan ClockSkewTolerance { get; set; } = TimeSpan.FromMinutes(1);

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
    /// No delay may be negative or longer than <see cref="MaxDuration"/>.
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
    /// Gets or sets the options used to serialize and deserialize payloads of this outbox.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default is <see cref="JsonSerializerOptions.Strict"/> with two more checks:
    /// <list type="bullet">
    ///    <item>Member names are exact C# matches</item>
    ///    <item>Enums are stored by name</item>
    /// </list>
    /// A payload that does not fit its type exactly cannot be read:
    /// </para>
    /// <list type="bullet">
    ///     <item>a constructor parameter without a default value is missing (<see cref="JsonSerializerOptions.RespectRequiredConstructorParameters"/>),</item>
    ///     <item>a non-nullable property outside the constructor is missing,</item>
    ///     <item>a non-nullable member holds null (<see cref="JsonSerializerOptions.RespectNullableAnnotations"/>),</item>
    ///     <item>a member the type does not have is present (<see cref="JsonUnmappedMemberHandling.Disallow"/>),</item>
    ///     <item>a member appears twice (<see cref="JsonSerializerOptions.AllowDuplicateProperties"/>),</item>
    ///     <item>a name differs in case (<see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/>),</item>
    ///     <item>a number is written as a string (<see cref="JsonNumberHandling.Strict"/>),</item>
    ///     <item>an enum value is not defined by its enum, or is written as a number (<see cref="JsonStringEnumConverter"/>).</item>
    /// </list>
    /// <para>
    /// Such a message is retried and dead-lettered, never handed to its handler with null or zero members.
    /// Enqueueing a message with null in a non-nullable member, or with an undefined enum value, throws.
    /// Elements of a collection and values of a dictionary are not checked for null.
    /// We want to avoid null reference exception and possible attacks/misuse of the handler.
    /// </para>
    /// <para>
    /// To customize it, copy the default: <c>options.SerializerOptions = new JsonSerializerOptions(options.SerializerOptions) { ... }</c>.
    /// </para>
    /// <para>
    /// Changing this after rows exist can make stored payloads unreadable, which dead-letters them.
    /// </para>
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; set; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Creates the default of <see cref="SerializerOptions"/>.
    /// </summary>
    /// <returns><see cref="JsonSerializerOptions.Strict"/>, with enums stored by name and every non-nullable property required.</returns>
    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Strict)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { RequireNonNullableProperties },
            },
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        serializerOptions.MakeReadOnly();

        return serializerOptions;
    }

    /// <summary>
    /// Makes every non-nullable property outside the constructor required, so a payload without it is refused.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonSerializerOptions.RespectRequiredConstructorParameters"/> covers the constructor parameters,
    /// and <see cref="JsonSerializerOptions.RespectNullableAnnotations"/> only an explicit null.
    /// </remarks>
    /// <param name="typeInfo">The contract of a type being serialized.</param>
    private static void RequireNonNullableProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            var isOptional = property.AssociatedParameter is not null // already decided by its constructor parameter
                || property.Set is null // never read
                || property.IsExtensionData
                || property.IsSetNullable;
            if (!isOptional)
            {
                property.IsRequired = true;
            }
        }
    }
}
