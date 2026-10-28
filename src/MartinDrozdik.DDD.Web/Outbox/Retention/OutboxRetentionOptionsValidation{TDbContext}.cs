using MartinDrozdik.DDD.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// Validates <see cref="OutboxRetentionOptions{TDbContext}"/> with <see cref="OutboxRetentionOptionsValidator{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
internal sealed class OutboxRetentionOptionsValidation<TDbContext> : IValidateOptions<OutboxRetentionOptions<TDbContext>>
    where TDbContext : DbContext
{
    private static readonly OutboxRetentionOptionsValidator<TDbContext> s_validator = new();
    private static readonly string s_contextName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OutboxRetentionOptions<TDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for the retention of the outbox of {s_contextName}.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
