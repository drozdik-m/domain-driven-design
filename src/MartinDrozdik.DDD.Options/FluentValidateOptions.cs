using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Options;

/// <summary>
/// Verifies <see cref="IValidatedAppOptions{TOptions}"/> with their own <see cref="IValidatedAppOptions{TOptions}.Validator"/>.
/// </summary>
/// <typeparam name="TOptions">The type of the option to validate.</typeparam>
public class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class, IValidatedAppOptions<TOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Do the validation
        var validator = TOptions.Validator;
        var result = validator.Validate(options);

        // Check results
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for {options.GetType().Name}.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
