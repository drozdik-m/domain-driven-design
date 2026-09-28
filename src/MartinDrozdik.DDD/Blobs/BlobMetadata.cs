using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// Small immutable key/value tags stored alongside a blob, persisted as a JSON.
/// </summary>
/// <remarks>
/// <para>
/// Meant for facts about the content that the library cannot know (f.e. width, page_count, ...).
/// Anything the application queries on deserves a real column on a real entity of its own.
/// Keys are lowercase only, because the cloud backends ignore case: Amazon S3 lowercases them and Azure Blob Storage matches them without regard to case.
/// A key therefore reads back exactly as it was written, whatever the store.
/// Reading is case-insensitive, so looking up <c>Width</c> finds <c>width</c>.
/// </para>
/// <para>
/// The limits are deliberately close to what Amazon S3 and Azure Blob Storage allow for user metadata,
/// so a future store that forwards them to a cloud backend cannot be handed something it must reject.
/// </para>
/// </remarks>
[DebuggerDisplay("{Values.Count} entries")]
public sealed class BlobMetadata : ValueObject
{
    /// <summary>
    /// The maximum number of entries.
    /// </summary>
    public const int MaxEntries = 32;

    /// <summary>
    /// The maximum number of characters of a key.
    /// </summary>
    public const int MaxKeyLength = 64;

    /// <summary>
    /// The maximum number of characters of a value.
    /// </summary>
    public const int MaxValueLength = 1024;

    /// <summary>
    /// The maximum number of characters of all keys and values together.
    /// </summary>
    public const int MaxTotalLength = 8 * 1024;

    /// <summary>
    /// The comparer of keys when reading. Keys are stored lowercase, so it only matters for lookups.
    /// </summary>
    private static readonly StringComparer s_keyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, string> _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobMetadata"/> class.
    /// </summary>
    /// <param name="values">The already validated entries.</param>
    private BlobMetadata(Dictionary<string, string> values)
    {
        _values = values;
    }

    /// <summary>
    /// Gets metadata with no entries. The non-null default of every blob.
    /// </summary>
    public static BlobMetadata Empty { get; } = new(new Dictionary<string, string>(s_keyComparer));

    /// <summary>
    /// Gets the entries. Keys are lowercase, looked up case-insensitively.
    /// </summary>
    public IReadOnlyDictionary<string, string> Values => _values;

    /// <summary>
    /// Gets the value stored under a key, or null when there is none.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <returns>The value, or null.</returns>
    public string? this[string key] => _values.GetValueOrDefault(key);

    /// <summary>
    /// Creates metadata from a set of entries.
    /// </summary>
    /// <param name="values">The entries.</param>
    /// <returns>The metadata, or an <see cref="ErrorCodes.InvalidObject"/> error when an entry, or the set as a whole, breaks one of the limits.</returns>
    public static Result<BlobMetadata, Error> Create(IEnumerable<KeyValuePair<string, string>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var entries = new Dictionary<string, string>(s_keyComparer);

        foreach (var entry in values)
        {
            if (ValidateEntry(entry.Key, entry.Value) is { } violation)
            {
                return BlobErrors.Invalid(nameof(Values), violation);
            }

            if (!entries.TryAdd(entry.Key, entry.Value))
            {
                return BlobErrors.Invalid(nameof(Values), $"The blob metadata key '{entry.Key}' was given more than once.");
            }
        }

        return FromValidEntries(entries);
    }

    /// <summary>
    /// Reads the value stored under a key.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="value">The value found, or null.</param>
    /// <returns>True when the key was present, else false.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _values.TryGetValue(key, out value);

    /// <summary>
    /// Returns a copy with a key set, replacing any value already stored under it.
    /// </summary>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>The new metadata, or an <see cref="ErrorCodes.InvalidObject"/> error when the entry, or the result as a whole, breaks one of the limits.</returns>
    public Result<BlobMetadata, Error> With(string key, string value)
    {
        if (ValidateEntry(key, value) is { } violation)
        {
            return BlobErrors.Invalid(nameof(Values), violation);
        }

        var entries = new Dictionary<string, string>(_values, s_keyComparer)
        {
            [key] = value,
        };

        return FromValidEntries(entries);
    }

    /// <summary>
    /// Returns a copy with a key removed.
    /// </summary>
    /// <remarks>
    /// Removing a key that is not there returns metadata equal to this one.
    /// </remarks>
    /// <param name="key">The key to remove.</param>
    /// <returns>The new metadata.</returns>
    public BlobMetadata Without(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_values.ContainsKey(key))
        {
            return this;
        }

        // Removing only ever shrinks the set, so it cannot break a limit
        var entries = new Dictionary<string, string>(_values, s_keyComparer);
        entries.Remove(key);

        return new BlobMetadata(entries);
    }

    /// <inheritdoc />
    public override string ToString() => $"{_values.Count} entries";

    /// <summary>
    /// Yields the entries in key order, so two sets holding the same pairs are equal however they were built.
    /// </summary>
    /// <returns>The components deciding equality.</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var entry in _values.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            yield return entry.Key;
            yield return entry.Value;
        }
    }

    /// <summary>
    /// Wraps entries that each passed <see cref="ValidateEntry"/>, once the set as a whole is checked.
    /// </summary>
    /// <param name="entries">The entries, owned by the new instance from here on.</param>
    /// <returns>The metadata, or an <see cref="ErrorCodes.InvalidObject"/> error when the set breaks one of the limits.</returns>
    private static Result<BlobMetadata, Error> FromValidEntries(Dictionary<string, string> entries)
    {
        if (ValidateSet(entries) is { } violation)
        {
            return BlobErrors.Invalid(nameof(Values), violation);
        }

        return new BlobMetadata(entries);
    }

    /// <summary>
    /// Checks a single entry against the limits.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to check.</param>
    /// <returns>The description of the violated rule, or null when the entry fits.</returns>
    private static string? ValidateEntry(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "A blob metadata key must not be empty.";
        }

        if (value is null)
        {
            return $"The blob metadata value under '{key}' must not be null.";
        }

        if (key.Length > MaxKeyLength)
        {
            return $"A blob metadata key must be at most {MaxKeyLength} characters long, but '{key}' is {key.Length}.";
        }

        if (!key.All(IsSupportedKeyCharacter))
        {
            return $"A blob metadata key may only contain lowercase letters, digits, '.', '_' and '-', but '{key}' does not.";
        }

        if (value.Length > MaxValueLength)
        {
            return $"A blob metadata value must be at most {MaxValueLength} characters long, but the one under '{key}' is {value.Length}.";
        }

        return null;
    }

    /// <summary>
    /// Checks the entries together against the limits.
    /// </summary>
    /// <param name="entries">The entries to check.</param>
    /// <returns>The description of the violated rule, or null when the set fits.</returns>
    private static string? ValidateSet(Dictionary<string, string> entries)
    {
        if (entries.Count > MaxEntries)
        {
            return $"Blob metadata may hold at most {MaxEntries} entries, but {entries.Count} were given.";
        }

        var total = entries.Sum(entry => entry.Key.Length + entry.Value.Length);
        if (total > MaxTotalLength)
        {
            return $"Blob metadata may hold at most {MaxTotalLength} characters in total, but {total} were given. Store the content itself as a blob instead.";
        }

        return null;
    }

    /// <summary>
    /// Decides whether a character may appear in a key.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is supported, else false.</returns>
    private static bool IsSupportedKeyCharacter(char character)
        => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '.' or '_' or '-';
}
