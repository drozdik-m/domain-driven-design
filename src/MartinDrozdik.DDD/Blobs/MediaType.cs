using System.Diagnostics;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Results.Exceptions;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// The media type of a blob, such as <c>image/png</c>.
/// </summary>
/// <remarks>
/// Declared by the programmer, never guessed from the content.
/// A store keeps bytes; deciding what they mean is a decision the application makes and is accountable for.
/// </remarks>
[DebuggerDisplay("{Value}")]
public sealed class MediaType : ValueObject
{
    /// <summary>
    /// The maximum number of characters of a <see cref="Value"/>.
    /// </summary>
    public const int MaxLength = 255;

    /// <summary>
    /// The type to fall back to when nothing more specific is known.
    /// </summary>
    /// <remarks>
    /// Served to a browser, it means "download this, I am not telling you what it is", which is the safe default.
    /// </remarks>
    public static readonly MediaType OctetStream = new("application", "octet-stream");

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaType"/> class.
    /// </summary>
    /// <param name="type">The already validated part before the slash, lowercase.</param>
    /// <param name="subType">The already validated part after the slash, lowercase.</param>
    private MediaType(string type, string subType)
    {
        Type = type;
        SubType = subType;
        Value = $"{type}/{subType}";
    }

    /// <summary>
    /// Gets the media type, lowercase and without parameters.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the part before the slash, such as <c>image</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the part after the slash, such as <c>png</c>.
    /// </summary>
    public string SubType { get; }

    /// <summary>
    /// An implicit casting from <see cref="string"/> to <see cref="MediaType"/>.
    /// </summary>
    /// <remarks>
    /// For values known to be valid, like literals. Use <see cref="Create(string?)"/> for anything read from outside.
    /// </remarks>
    /// <param name="value">The media type.</param>
    /// <exception cref="ResultFailureException{E}">The value is not a valid media type.</exception>
    public static implicit operator MediaType(string value) => Create(value).Value;

    /// <summary>
    /// Creates a media type from a value that may not be valid, such as a declared <c>Content-Type</c>.
    /// </summary>
    /// <param name="value">The media type, with or without parameters.</param>
    /// <returns>The media type, or an <see cref="ErrorCodes.InvalidObject"/> error when the value is empty, too long, or not a <c>type/subtype</c> pair.</returns>
    public static Result<MediaType, Error> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BlobErrors.Invalid(nameof(Value), "A media type must not be empty.");
        }

        var essence = value.Split(';')[0].Trim().ToLowerInvariant();

        if (Validate(essence) is { } violation)
        {
            return BlobErrors.Invalid(nameof(Value), violation);
        }

        var separator = essence.IndexOf('/', StringComparison.Ordinal);
        return new MediaType(essence[..separator], essence[(separator + 1)..]);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Checks the essence of a media type against the rules.
    /// </summary>
    /// <param name="essence">The media type without parameters, lowercase.</param>
    /// <returns>The description of the violated rule, or null when the essence is valid.</returns>
    private static string? Validate(string essence)
    {
        if (essence.Length > MaxLength)
        {
            return $"A media type must be at most {MaxLength} characters long, but '{essence}' is {essence.Length}.";
        }

        var separator = essence.IndexOf('/', StringComparison.Ordinal);
        if (separator <= 0 || separator == essence.Length - 1 || essence.IndexOf('/', separator + 1) >= 0)
        {
            return $"A media type must be a 'type/subtype' pair, but '{essence}' is not.";
        }

        if (!essence.All(IsSupported))
        {
            return $"A media type may only contain letters, digits, '/', '.', '+', '_' and '-', but '{essence}' does not.";
        }

        return null;
    }

    /// <summary>
    /// Decides whether a character may appear in a <see cref="Value"/>.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is supported, else false.</returns>
    private static bool IsSupported(char character)
        => char.IsAsciiLetterOrDigit(character) || character is '/' or '.' or '+' or '_' or '-';
}
