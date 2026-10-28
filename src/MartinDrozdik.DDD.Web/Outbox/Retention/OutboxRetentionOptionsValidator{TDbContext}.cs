using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// Validates <see cref="OutboxRetentionOptions{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
internal sealed class OutboxRetentionOptionsValidator<TDbContext> : AbstractValidator<OutboxRetentionOptions<TDbContext>>
    where TDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRetentionOptionsValidator{TDbContext}"/> class.
    /// </summary>
    public OutboxRetentionOptionsValidator()
    {
        RuleFor(x => x.Retention)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox retention must be greater than zero. To keep delivered messages forever, do not add the retention.");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Outbox retention batch size must be greater than zero, otherwise no delivered message would ever be deleted.");
    }
}
