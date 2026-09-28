using System.Diagnostics;
using FluentValidation;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Results;

namespace MartinDrozdik.DDD.Blobs.Models;

/// <summary>
/// The catalogue row of one stored blob, whose content is <b>immutable</b>.
/// Only information, no content.
/// The content is stored via <see cref="IBlobStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Replacing a file requires storing a new blob and enqueueing the deletion of the old one.
/// The metadata and the expiry are the only parts that change, each rotating <see cref="ConcurrencyStamp"/>.
/// </para>
/// <para>
/// Deliberately <b>not</b> an <see cref="Templates.IAggregateRoot{TIdentity}"/>.
/// It is an infrastructure row, not a domain entity.
/// </para>
/// <para>
/// All instants are UTC <see cref="DateTime"/> rather than <see cref="DateTimeOffset"/> because not all
/// Entity Framework Core providers can translate relational comparisons of <see cref="DateTimeOffset"/>,
/// and comparing <see cref="ExpiresAt"/> is the core of the sweep query.
/// </para>
/// </remarks>
[DebuggerDisplay("{OriginalFileName} ({Id})")]
public sealed class Blob
{
    /// <summary>
    /// The maximum number of characters of an <see cref="OriginalFileName"/>.
    /// </summary>
    /// <remarks>
    /// Longer than a <see cref="BlobName"/>, because this one is recorded as it arrived rather than sanitized.
    /// </remarks>
    public const int OriginalFileNameMaxLength = 260;

    /// <summary>
    /// The maximum number of characters of a <see cref="CreatedBy"/>.
    /// </summary>
    public const int CreatedByMaxLength = 256;

    /// <summary>
    /// The maximum number of characters of an <see cref="Extension"/>.
    /// </summary>
    public const int ExtensionMaxLength = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blob"/> class.
    /// </summary>
    /// <remarks>
    /// Can be used by Entity Framework Core, see <see cref="Create"/>.
    /// </remarks>
    private Blob()
    {
    }

    /// <summary>
    /// Gets the identity of the blob, which is also what its content is stored under.
    /// </summary>
    /// <remarks>
    /// A version 7 GUID.
    /// </remarks>
    public BlobId Id { get; private set; } = null!;

    /// <summary>
    /// Gets the container the blob lives in.
    /// </summary>
    public BlobContainer Container { get; private set; } = null!;

    /// <summary>
    /// Gets a sanitized copy of <see cref="OriginalFileName"/>, ready to offer as a download name.
    /// </summary>
    /// <remarks>
    /// Only catalogued - the content is stored under <see cref="Id"/>, never under a name.
    /// </remarks>
    public BlobName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the name the file arrived with, unsanitized.
    /// <b>Never used to build a path.</b>
    /// </summary>
    /// <remarks>
    /// Kept only so a download can be offered under the name the person recognises.
    /// </remarks>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the extension of <see cref="Name"/>, lowercase and without the leading dot, or null when the name has none.
    /// </summary>
    /// <remarks>
    /// Denormalized out of the name so "every PDF" is an indexable query rather than a scan.
    /// Null when the extension is longer than <see cref="ExtensionMaxLength"/>, which is then not really an extension.
    /// </remarks>
    public string? Extension { get; private set; }

    /// <summary>
    /// Gets the media type the content was declared as.
    /// </summary>
    public MediaType ContentType { get; private set; } = MediaType.OctetStream;

    /// <summary>
    /// Gets the size of the content, in bytes.
    /// </summary>
    /// <remarks>
    /// Measured while the content was written.
    /// </remarks>
    public long Size { get; private set; }

    /// <summary>
    /// Gets the hash of the content, or null when hashing was turned off.
    /// </summary>
    public BlobChecksum? Checksum { get; private set; }

    /// <summary>
    /// Gets the tags stored alongside the blob.
    /// </summary>
    public BlobMetadata Metadata { get; private set; } = BlobMetadata.Empty;

    /// <summary>
    /// Gets the UTC instant the blob was catalogued.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets who stored the blob, or null when the application did not say.
    /// </summary>
    /// <remarks>
    /// A plain string rather than a relationship, because the library cannot know what a user is here.
    /// </remarks>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// Gets the UTC instant after which a sweep may remove the blob, or null when it is kept indefinitely.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the optimistic concurrency token.
    /// </summary>
    /// <remarks>
    /// The metadata is the one mutable part of a blob, and two requests tagging the same file would otherwise silently overwrite one another.
    /// </remarks>
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the address of the content in the store.
    /// </summary>
    /// <remarks>
    /// Computed from the columns rather than stored.
    /// Every segment is already a column, and a second copy of the path is one more thing that could disagree with them.
    /// </remarks>
    public BlobKey Key => BlobKey.Create(Container, Id);

    /// <summary>
    /// Catalogues a blob whose content has already been written.
    /// </summary>
    /// <param name="parameters">What to catalogue.</param>
    /// <returns>The new row.</returns>
    /// <exception cref="BusinessRuleValidationException">
    /// The original file name is empty, the size is negative, or who stored it is longer than <see cref="CreatedByMaxLength"/>.
    /// </exception>
    public static Blob Create(CreateBlobParams parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Key);
        ArgumentNullException.ThrowIfNull(parameters.ContentType);
        ArgumentNullException.ThrowIfNull(parameters.Metadata);
        Validator.Instance.ValidateAndThrowBusiness(parameters);

        var name = BlobName.FromFileName(parameters.OriginalFileName);

        return new Blob
        {
            Id = parameters.Key.Id,
            Container = parameters.Key.Container,
            Name = name,
            OriginalFileName = Truncate(parameters.OriginalFileName, OriginalFileNameMaxLength),
            Extension = name.Extension is { Length: <= ExtensionMaxLength } extension ? extension : null,
            ContentType = parameters.ContentType,
            Size = parameters.Size,
            Checksum = parameters.Checksum,
            Metadata = parameters.Metadata,
            CreatedAt = parameters.CreatedAtUtc,
            CreatedBy = parameters.CreatedBy,
            ExpiresAt = parameters.ExpiresAtUtc,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
    }

    /// <summary>
    /// Stores a tag alongside the blob, replacing whatever was under the key.
    /// </summary>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>Success, or an <see cref="ErrorCodes.InvalidObject"/> error when the entry, or the metadata as a whole, breaks one of its limits.</returns>
    public UnitResult<Error> SetMetadata(string key, string value)
    {
        var metadata = Metadata.With(key, value);
        if (metadata.IsFailure)
        {
            return metadata.Error;
        }

        Metadata = metadata.Value;
        RotateConcurrencyStamp();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Removes a tag from the blob.
    /// </summary>
    /// <remarks>
    /// Removing a key that is not there is a success.
    /// </remarks>
    /// <param name="key">The key to remove.</param>
    public void RemoveMetadata(string key)
    {
        Metadata = Metadata.Without(key);
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Reads a tag stored alongside the blob.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="value">The value found, or null.</param>
    /// <returns>True when the key was present, else false.</returns>
    public bool TryGetMetadata(string key, out string? value) => Metadata.TryGetValue(key, out value);

    /// <summary>
    /// Changes when the sweep may remove the blob.
    /// </summary>
    /// <param name="expiresAtUtc">The UTC instant after which it may be swept, or null to keep it indefinitely.</param>
    public void SetExpiry(DateTime? expiresAtUtc)
    {
        ExpiresAt = expiresAtUtc;
        RotateConcurrencyStamp();
    }

    /// <summary>
    /// Builds the message that removes this blob's content once the current transaction has committed.
    /// </summary>
    /// <remarks>
    /// The message carries the address as plain strings and never the row, because by the time it is delivered the row is gone.
    /// </remarks>
    /// <returns>The message to enqueue.</returns>
    public DeleteBlobMessage ToDeleteMessage() => DeleteBlobMessage.For(Key);

    /// <summary>
    /// Shortens a value so it always fits its column.
    /// </summary>
    /// <param name="value">The value to shorten.</param>
    /// <param name="maxLength">The most characters the column holds.</param>
    /// <returns>The value, at most <paramref name="maxLength"/> characters long.</returns>
    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// Changes the concurrency token so a competing write of the same row is rejected.
    /// </summary>
    private void RotateConcurrencyStamp() => ConcurrencyStamp = Guid.CreateVersion7();

    /// <summary>
    /// Validator of the <see cref="CreateBlobParams"/> given to <see cref="Create"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="OriginalFileName"/> has no maximum length on purpose. It arrives from outside and anything is accepted,
    /// so it is shortened to fit its column instead.
    /// </remarks>
    private sealed class Validator : AbstractValidator<CreateBlobParams>
    {
        public Validator()
        {
            RuleFor(x => x.OriginalFileName).NotEmpty();
            RuleFor(x => x.Size).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CreatedBy).MaximumLength(CreatedByMaxLength);
        }

        public static Validator Instance { get; } = new();
    }
}
