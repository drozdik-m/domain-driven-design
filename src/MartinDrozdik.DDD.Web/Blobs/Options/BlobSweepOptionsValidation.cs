using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Validates <see cref="BlobSweepOptions"/> with <see cref="BlobSweepOptionsValidator"/>.
/// </summary>
internal sealed class BlobSweepOptionsValidation : IValidateOptions<BlobSweepOptions>
{
    private static readonly BlobSweepOptionsValidator s_validator = new();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BlobSweepOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for the blob sweep.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
