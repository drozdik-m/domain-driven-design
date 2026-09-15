using System.Diagnostics;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Outbox;

/// <summary>
/// The stable storage key of an outbox message type.
/// </summary>
/// <remarks>
/// It's used as the dependency injection key of the matching dispatcher.
/// The key must never change once rows carrying it exist or the messages will be dead-lettered.
/// Prefer a deliberate, storage-facing name such as <c>email.send.v1</c> over anything derived from a type name, which a rename would break.
/// <para>
/// End the key with a version segment such as <c>.v1</c>. It will help with a contract change. Trust me bro.
/// </para>
/// </remarks>
[DebuggerDisplay("{Key}")]
public sealed class OutboxMessageType : ValueObject
{
    /// <summary>
    /// The maximum number of characters of a <see cref="Key"/>.
    /// </summary>
    public const int MaxLength = 100;

    /// <summary>
    /// The placeholder used as the non-null default.
    /// </summary>
    public static readonly OutboxMessageType Empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessageType"/> class.
    /// </summary>
    /// <param name="key">The storage key of the message type.</param>
    /// <exception cref="ArgumentException">The key is empty, too long, or contains unsupported characters.</exception>
    public OutboxMessageType(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Length > MaxLength)
        {
            throw new ArgumentException($"An outbox message type must be at most {MaxLength} characters long, but '{key}' is {key.Length}.", nameof(key));
        }

        if (!key.All(IsSupported))
        {
            throw new ArgumentException($"An outbox message type may only contain letters, digits, '.', '_' and '-', but '{key}' does not.", nameof(key));
        }

        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessageType"/> class as the <see cref="Empty"/> placeholder.
    /// </summary>
    private OutboxMessageType()
    {
        Key = string.Empty;
    }

    /// <summary>
    /// Gets the storage key of the message type. Unique across all registered message types.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// An implicit casting from string to <see cref="OutboxMessageType"/>.
    /// </summary>
    /// <param name="key">The storage key of the message type.</param>
    public static implicit operator OutboxMessageType(string key) => new(key);

    /// <inheritdoc />
    public override string ToString() => Key;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }

    /// <summary>
    /// Decides whether a character may appear in a <see cref="Key"/>.
    /// The set is deliberately narrow so a key is always safe as both a column value and a service key.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is supported, else false.</returns>
    private static bool IsSupported(char character)
        => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-';
}
