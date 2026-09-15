using System.Diagnostics;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Models;

/// <summary>
/// A message stored in the EF outbox table, written within a transaction along domain changes.
/// Delivered afterwards by an <see cref="IOutboxProcessor"/>.
/// </summary>
/// <remarks>
/// <para>
/// All instants are UTC <see cref="DateTime"/> rather than <see cref="DateTimeOffset"/> because not all
/// Entity Framework Core providers can translate relational comparisons of <see cref="DateTimeOffset"/>,
/// and comparing <see cref="AvailableAt"/> is the core of every dispatcher query.
/// </para>
/// </remarks>
[DebuggerDisplay("{MessageType} ({Id})")]
public sealed class OutboxMessage
{
    /// <summary>
    /// The maximum number of characters of <see cref="LastError"/>. Longer errors are truncated.
    /// </summary>
    public const int LastErrorMaxLength = 2000;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessage"/> class.
    /// Used by Entity Framework Core only, see <see cref="Create"/>.
    /// </summary>
    private OutboxMessage()
    {
    }

    /// <summary>
    /// Gets the identity of the message.
    /// </summary>
    /// <remarks>
    /// Version 7 GUID.
    /// </remarks>
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the type of the message, used to find the handler that delivers it.
    /// </summary>
    public OutboxMessageType MessageType { get; private set; } = OutboxMessageType.Empty;

    /// <summary>
    /// Gets the serialized body of the message.
    /// </summary>
    public OutboxPayload Payload { get; private set; } = OutboxPayload.s_empty;

    /// <summary>
    /// Gets the UTC instant the message was enqueued.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Gets the UTC instant from which the message may be dispatched.
    /// Equal to <see cref="OccurredAt"/> until a failed attempt pushes it forward by a retry delay.
    /// </summary>
    public DateTime AvailableAt { get; private set; }

    /// <summary>
    /// Gets the UTC instant the message was delivered, or null while it is still pending.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Gets the UTC instant the message was dead-lettered, or null while it may still be retried.
    /// A dead-lettered message is never dispatched again.
    /// </summary>
    public DateTime? FailedAt { get; private set; }

    /// <summary>
    /// Gets the number of delivery attempts made so far.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Gets the error of the most recent failed attempt, truncated to <see cref="LastErrorMaxLength"/>.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Gets the identity of the processor currently holding the message, or null when it is free.
    /// </summary>
    public Guid? ClaimedBy { get; private set; }

    /// <summary>
    /// Gets the UTC instant the current claim expires.
    /// Once it passes, another processor may take the message (even though <see cref="ClaimedBy"/> is not null).
    /// Prevents crashed processors from blocking a message indefinitely.
    /// </summary>
    public DateTime? ClaimedUntil { get; private set; }

    /// <summary>
    /// Gets the optimistic concurrency token.
    /// It is what makes claiming safe - two processors racing for the same message both write a
    /// lease, and the loser is rejected with a <see cref="DbUpdateConcurrencyException"/>.
    /// </summary>
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>
    /// Creates a message that is immediately available for delivery.
    /// </summary>
    /// <param name="messageType">The type of the message.</param>
    /// <param name="payload">The serialized body of the message.</param>
    /// <param name="occurredAtUtc">The UTC time the message was enqueued.</param>
    /// <returns>The new message.</returns>
    public static OutboxMessage Create(OutboxMessageType messageType, OutboxPayload payload, DateTime occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);

        var result = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            MessageType = messageType,
            Payload = payload,
            OccurredAt = occurredAtUtc,
            AvailableAt = occurredAtUtc,
        };

        result.RotateConcurrencyStamp();
        return result;
    }

    /// <summary>
    /// Takes a lease on the message so no other processor delivers it concurrently.
    /// </summary>
    /// <param name="claimId">The identity of the claiming processor.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="leaseDuration">How long the claim is honoured before the message may be taken again.</param>
    public void Claim(Guid claimId, DateTime nowUtc, TimeSpan leaseDuration)
    {
        EnsurePending(nameof(Claim));

        ClaimedBy = claimId;
        ClaimedUntil = nowUtc + leaseDuration;
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Marks the message as delivered. It is never dispatched again.
    /// </summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public void MarkProcessed(DateTime nowUtc)
    {
        EnsurePending(nameof(MarkProcessed));

        Attempts++;
        ProcessedAt = nowUtc;
        LastError = null;
        ReleaseClaim();
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Records a failed attempt and schedules the next one.
    /// </summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="delay">How long to wait before the message becomes available again.</param>
    /// <param name="error">The error of the failed attempt.</param>
    public void MarkRetrying(DateTime nowUtc, TimeSpan delay, string error)
    {
        EnsurePending(nameof(MarkRetrying));

        Attempts++;
        AvailableAt = nowUtc + delay;
        LastError = Truncate(error);
        ReleaseClaim();
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Dead-letters the message. It is never dispatched again and stays in the table for inspection.
    /// </summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="error">The error of the final attempt.</param>
    public void MarkFailed(DateTime nowUtc, string error)
    {
        EnsurePending(nameof(MarkFailed));

        Attempts++;
        FailedAt = nowUtc;
        LastError = Truncate(error);
        ReleaseClaim();
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Shortens an error so it always fits the column.
    /// </summary>
    /// <param name="error">The error to shorten.</param>
    /// <returns>The error, at most <see cref="LastErrorMaxLength"/> characters long.</returns>
    private static string Truncate(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Length <= LastErrorMaxLength
            ? error
            : error[..LastErrorMaxLength];
    }

    /// <summary>
    /// Throws when the message has already reached a terminal state.
    /// </summary>
    /// <param name="operation">The name of the attempted operation, reported in the error.</param>
    private void EnsurePending(string operation)
    {
        if (ProcessedAt is null && FailedAt is null)
        {
            return;
        }

        var state = ProcessedAt is not null ? "processed" : "dead-lettered";
        throw new ErrorBuilder()
            .WithCode("OutboxMessageNotPending")
            .WithMessage($"Cannot {operation} outbox message {Id} because it is already {state}.")
            .WithDetail(nameof(MessageType), MessageType.Key)
            .WithDetail("Operation", operation)
            .BuildValidationException();
    }

    /// <summary>
    /// Frees the message so another processor may take it.
    /// </summary>
    private void ReleaseClaim()
    {
        ClaimedBy = null;
        ClaimedUntil = null;
    }

    /// <summary>
    /// Changes the concurrency token so a competing write of the same row is rejected.
    /// </summary>
    private void RotateConcurrencyStamp() => ConcurrencyStamp = Guid.CreateVersion7();
}
