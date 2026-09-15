using System.Diagnostics;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Web.Outbox.Models;

/// <summary>
/// The serialized body of an outbox message.
/// Written to and read from the message payload column.
/// </summary>
/// <remarks>
/// Unbounded here but should be generally bounded by <see cref="Options.OutboxOptions.MaxPayloadLength"/> to avoid filling the database with large content.
/// </remarks>
[DebuggerDisplay("{Value}")]
public sealed class OutboxPayload : ValueObject
{
    /// <summary>
    /// The placeholdernon-null default before Entity Framework materializes the real value.
    /// </summary>
    internal static readonly OutboxPayload s_empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPayload"/> class.
    /// </summary>
    /// <param name="value">The serialized message body.</param>
    /// <exception cref="ArgumentException">The value is null, empty or whitespace.</exception>
    public OutboxPayload(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPayload"/> class as the <see cref="s_empty"/> placeholder.
    /// </summary>
    private OutboxPayload()
    {
        Value = string.Empty;
    }

    /// <summary>
    /// Gets the serialized message body.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the number of characters of the <see cref="Value"/>.
    /// </summary>
    public int Length => Value.Length;

    /// <summary>
    /// An implicit casting from string to <see cref="OutboxPayload"/>.
    /// </summary>
    /// <param name="value">The serialized message body.</param>
    public static implicit operator OutboxPayload(string value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
