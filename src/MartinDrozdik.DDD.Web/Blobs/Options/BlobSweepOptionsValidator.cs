using FluentValidation;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Validates <see cref="BlobSweepOptions"/>.
/// </summary>
internal sealed class BlobSweepOptionsValidator : AbstractValidator<BlobSweepOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobSweepOptionsValidator"/> class.
    /// </summary>
    public BlobSweepOptionsValidator()
    {
        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("The sweep batch size must be greater than zero, otherwise a sweep would never remove anything.");

        RuleFor(x => x.MaxPasses)
            .GreaterThan(0)
            .WithMessage("The maximum number of sweep passes must be greater than zero, otherwise a sweep would never remove anything.");
    }
}
