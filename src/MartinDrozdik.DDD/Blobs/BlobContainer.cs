using System.Diagnostics;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Results.Exceptions;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Blobs;

/// <summary>
/// The logical container a blob lives in - the top-level grouping of a store.
/// Only lowercase letters, digits and <c>-</c> allowed.
/// </summary>
/// <remarks>
/// The bucket of Amazon S3, the container of Azure Blob Storage, a folder directly under the configured root of a file store.
/// Name it after the kind of content rather than the owner: <c>invoices</c>, <c>avatars</c>.
/// </remarks>
[DebuggerDisplay("{Name}")]
public sealed class BlobContainer : ValueObject
{
    /// <summary>
    /// The maximum number of characters of a <see cref="Name"/>.
    /// </summary>
    /// <remarks>
    /// Matches the Amazon S3 bucket-name limit, the tightest of the backends this could grow to support.
    /// </remarks>
    public const int MaxLength = 63;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContainer"/> class.
    /// </summary>
    /// <param name="name">The already validated name of the container.</param>
    private BlobContainer(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the container.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// An implicit casting from <see cref="string"/> to <see cref="BlobContainer"/>.
    /// </summary>
    /// <param name="name">The name of the container.</param>
    /// <exception cref="ResultFailureException{E}">The name is not a valid one.</exception>
    public static implicit operator BlobContainer(string name) => Create(name).Value;

    /// <summary>
    /// Creates a container from a name.
    /// </summary>
    /// <param name="name">The name of the container.</param>
    /// <returns>The container, or an <see cref="ErrorCodes.InvalidObject"/> error when the name is not a valid one.</returns>
    public static Result<BlobContainer, Error> Create(string? name)
    {
        if (Validate(name) is { } violation)
        {
            return BlobErrors.Invalid(nameof(Name), violation);
        }

        return new BlobContainer(name!);
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
    }

    /// <summary>
    /// Checks a name against the rules of a container.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>The description of the violated rule, or null when the name is valid.</returns>
    private static string? Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A blob container name must not be empty.";
        }

        if (name.Length > MaxLength)
        {
            return $"A blob container name must be at most {MaxLength} characters long, but '{name}' is {name.Length}.";
        }

        if (!name.All(IsSupported))
        {
            return $"A blob container name may only contain lowercase letters, digits and '-', but '{name}' does not.";
        }

        if (name[0] is '-' || name[^1] is '-')
        {
            return $"A blob container name may not start or end with '-', but '{name}' does.";
        }

        return null;
    }

    /// <summary>
    /// Decides whether a character may appear in a <see cref="Name"/>.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is supported, else false.</returns>
    private static bool IsSupported(char character)
        => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '-';
}
