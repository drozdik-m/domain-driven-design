using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

/// <summary>
/// Validates <see cref="OutboxOptions"/> with <see cref="OutboxOptionsValidator"/>.
/// </summary>
internal sealed class OutboxOptionsValidation : IValidateOptions<OutboxOptions>
{
    private static readonly OutboxOptionsValidator s_validator = new();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for the outbox.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
