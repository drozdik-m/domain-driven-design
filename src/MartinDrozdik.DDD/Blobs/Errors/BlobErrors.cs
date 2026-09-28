using System.Globalization;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;

namespace MartinDrozdik.DDD.Blobs.Errors;

/// <summary>
/// Builds the errors blob storage reports.
/// </summary>
public static class BlobErrors
{
    /// <summary>
    /// Reports that no blob is catalogued under an identity.
    /// </summary>
    /// <param name="id">The identity that was looked up.</param>
    /// <returns>The <see cref="Error"/>.</returns>
    public static Error NotFound(BlobId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return new ErrorBuilder()
            .WithCode(BlobErrorCodes.BlobNotFound)
            .WithMessage($"No blob is stored under '{id}'.")
            .WithDetail(nameof(BlobId), id.ToString())
            .Build();
    }

    /// <summary>
    /// Reports that a blob is catalogued but its content is gone from the store.
    /// </summary>
    /// <remarks>
    /// The store may only be unreachable or gone.
    /// </remarks>
    /// <param name="key">The address whose content is missing.</param>
    /// <returns>The <see cref="Error"/>.</returns>
    public static Error ContentMissing(BlobKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return new ErrorBuilder()
            .WithCode(BlobErrorCodes.BlobContentMissing)
            .WithMessage($"The blob '{key.Id}' is catalogued, but its content is not in the store.")
            .WithDetail(nameof(BlobKey), key.Path)
            .Build();
    }

    /// <summary>
    /// Reports that content is larger than the configured limit.
    /// </summary>
    /// <param name="maxSize">The largest allowed size, in bytes.</param>
    /// <returns>The <see cref="Error"/>.</returns>
    public static Error TooLarge(long maxSize)
    {
        return new ErrorBuilder()
            .WithCode(BlobErrorCodes.BlobTooLarge)
            .WithMessage($"The content is larger than the allowed {maxSize} bytes.")
            .WithDetail(nameof(maxSize), maxSize.ToString(CultureInfo.InvariantCulture))
            .Build();
    }

    /// <summary>
    /// Reports that a media type is not among the allowed ones.
    /// </summary>
    /// <param name="contentType">The media type that was refused.</param>
    /// <returns>The <see cref="Error"/>.</returns>
    public static Error ContentTypeNotAllowed(MediaType contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        return new ErrorBuilder()
            .WithCode(BlobErrorCodes.BlobTypeNotAllowed)
            .WithMessage($"Files of type '{contentType}' are not accepted here.")
            .WithDetail(nameof(MediaType), contentType.Value)
            .Build();
    }

    /// <summary>
    /// Reports that an extension is not among the allowed ones.
    /// </summary>
    /// <param name="extension">The extension that was refused, without the leading dot, or null when the name had none.</param>
    /// <returns>The <see cref="Error"/>.</returns>
    public static Error ExtensionNotAllowed(string? extension)
    {
        var builder = new ErrorBuilder()
            .WithCode(BlobErrorCodes.BlobTypeNotAllowed)
            .WithMessage(extension is null
                ? "Files without an extension are not accepted here."
                : $"Files with the extension '{extension}' are not accepted here.");

        if (extension is not null)
        {
            builder.WithDetail(nameof(extension), extension);
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates the error of a value that violates the rules of a blob value object.
    /// </summary>
    /// <param name="property">The name of the property the value was meant for.</param>
    /// <param name="violation">The description of the violated rule.</param>
    /// <returns>An <see cref="ErrorCodes.InvalidObject"/> error.</returns>
    internal static Error Invalid(string property, string violation)
        => new ErrorBuilder()
            .WithCode(ErrorCodes.InvalidObject)
            .WithMessage(WellKnownErrorMessages.InvariantError)
            .WithDetail(property, violation)
            .Build();
}
