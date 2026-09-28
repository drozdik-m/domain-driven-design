using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Validates <see cref="BlobContainerOptions"/> with <see cref="BlobContainerOptionsValidator"/>.
/// </summary>
/// <remarks>
/// Runs for every named instance, so each failure names the container it belongs to.
/// </remarks>
internal sealed class BlobContainerOptionsValidation : IValidateOptions<BlobContainerOptions>
{
    private static readonly BlobContainerOptionsValidator s_validator = new();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BlobContainerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for the blob container '{name}'.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
