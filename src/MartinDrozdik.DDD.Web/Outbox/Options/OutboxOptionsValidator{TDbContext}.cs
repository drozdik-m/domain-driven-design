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
        var maxDuration = OutboxOptions<TDbContext>.MaxDuration;

        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Outbox batch size must be greater than zero, otherwise no message would ever be delivered.");

        RuleFor(x => x.LeaseDuration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox lease duration must be greater than zero, otherwise a claim would expire the moment it is taken.")
            .LessThanOrEqualTo(maxDuration)
            .WithMessage($"Outbox lease duration must not be longer than {maxDuration.TotalDays} days.");

        RuleFor(x => x.ReservationDuration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox reservation duration must be greater than zero, otherwise a batch would be released the moment it is reserved.");

        RuleFor(x => x.ReservationDuration)
            .LessThanOrEqualTo(x => x.LeaseDuration)
            .When(x => x.LeaseDuration > TimeSpan.Zero)
            .WithMessage("Outbox reservation duration must not exceed the lease duration, a message waiting in a batch is held no longer than one being delivered.");

        RuleFor(x => x.ClockSkewTolerance)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Outbox clock skew tolerance must not be negative, otherwise a lease would expire before its end.")
            .LessThanOrEqualTo(maxDuration)
            .WithMessage($"Outbox clock skew tolerance must not be longer than {maxDuration.TotalDays} days.");

        RuleFor(x => x.RetryDelays)
            .NotNull()
            .WithMessage("Outbox retry delays must not be null. Use an empty list to dead-letter on the first failure.");

        RuleForEach(x => x.RetryDelays)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Every outbox retry delay must not be negative.")
            .LessThanOrEqualTo(maxDuration)
            .WithMessage($"Every outbox retry delay must not be longer than {maxDuration.TotalDays} days. Use fewer retry delays to dead-letter sooner.");

        RuleFor(x => x.MaxPayloadLength)
            .GreaterThan(0)
            .When(x => x.MaxPayloadLength.HasValue)
            .WithMessage("Outbox maximum payload length must be greater than zero when set. Leave it null for no limit.");

        RuleFor(x => x.SerializerOptions)
            .NotNull()
            .WithMessage("Outbox serializer options must not be null.");
    }
}
