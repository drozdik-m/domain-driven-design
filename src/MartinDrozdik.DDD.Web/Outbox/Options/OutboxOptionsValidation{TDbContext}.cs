using MartinDrozdik.DDD.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

/// <summary>
/// Validates <see cref="OutboxOptions{TDbContext}"/> with <see cref="OutboxOptionsValidator{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
internal sealed class OutboxOptionsValidation<TDbContext> : IValidateOptions<OutboxOptions<TDbContext>>
    where TDbContext : DbContext
{
    private static readonly OutboxOptionsValidator<TDbContext> s_validator = new();
    private static readonly string s_contextName = typeof(TDbContext).GetReadableName();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OutboxOptions<TDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for the outbox of {s_contextName}.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
