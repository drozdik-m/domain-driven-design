using FluentValidation;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Validates <see cref="BlobContainerOptions"/>.
/// </summary>
internal sealed class BlobContainerOptionsValidator : AbstractValidator<BlobContainerOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContainerOptionsValidator"/> class.
    /// </summary>
    public BlobContainerOptionsValidator()
    {
        RuleFor(x => x.MaxSize)
            .GreaterThan(0)
            .When(x => x.MaxSize is not null)
            .WithMessage("The blob size limit must be greater than zero, otherwise nothing could ever be stored.");

        RuleFor(x => x.OrphanGracePeriod)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("The orphan grace period must be greater than zero, or the outbox would delete uploads whose transaction has not committed yet.");

        RuleForEach(x => x.AllowedExtensions)
            .Must(extension => !extension.StartsWith('.'))
            .When(x => x.AllowedExtensions is not null)
            .WithMessage("An allowed extension is written without its leading dot, the way a blob records it.");

        RuleForEach(x => x.AllowedExtensions)
            .Must(extension => extension.All(character => char.IsAsciiDigit(character) || char.IsAsciiLetterLower(character)))
            .When(x => x.AllowedExtensions is not null)
            .WithMessage("An allowed extension is written in lowercase, the way a blob records it.");
    }
}
