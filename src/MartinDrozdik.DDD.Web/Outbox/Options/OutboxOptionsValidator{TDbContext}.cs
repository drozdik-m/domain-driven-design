using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

/// <summary>
/// Validates <see cref="OutboxOptions{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
internal sealed class OutboxOptionsValidator<TDbContext> : AbstractValidator<OutboxOptions<TDbContext>>
    where TDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxOptionsValidator{TDbContext}"/> class.
    /// </summary>
    public OutboxOptionsValidator()
    {
        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Outbox batch size must be greater than zero, otherwise no message would ever be delivered.");

        RuleFor(x => x.LeaseDuration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox lease duration must be greater than zero, otherwise a claim would expire the moment it is taken.");

        RuleFor(x => x.ReservationDuration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox reservation duration must be greater than zero, otherwise a batch would be released the moment it is reserved.");

        RuleFor(x => x.ReservationDuration)
            .LessThanOrEqualTo(x => x.LeaseDuration)
            .When(x => x.LeaseDuration > TimeSpan.Zero)
            .WithMessage("Outbox reservation duration must not exceed the lease duration, a message waiting in a batch is held no longer than one being delivered.");

        RuleFor(x => x.RetryDelays)
            .NotNull()
            .WithMessage("Outbox retry delays must not be null. Use an empty list to dead-letter on the first failure.");

        RuleForEach(x => x.RetryDelays)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Every outbox retry delay must not be negative.");

        RuleFor(x => x.MaxPayloadLength)
            .GreaterThan(0)
            .When(x => x.MaxPayloadLength.HasValue)
            .WithMessage("Outbox maximum payload length must be greater than zero when set. Leave it null for no limit.");

        RuleFor(x => x.Retention)
            .GreaterThan(TimeSpan.Zero)
            .When(x => x.Retention.HasValue)
            .WithMessage("Outbox retention must be greater than zero when set. Leave it null to keep delivered messages forever.");

        RuleFor(x => x.SerializerOptions)
            .NotNull()
            .WithMessage("Outbox serializer options must not be null.");
    }
}
