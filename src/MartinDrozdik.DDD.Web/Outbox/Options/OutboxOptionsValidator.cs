using FluentValidation;

namespace MartinDrozdik.DDD.Web.Outbox.Options;

/// <summary>
/// Validates <see cref="OutboxOptions"/>.
/// </summary>
internal sealed class OutboxOptionsValidator : AbstractValidator<OutboxOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxOptionsValidator"/> class.
    /// </summary>
    public OutboxOptionsValidator()
    {
        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Outbox batch size must be greater than zero, otherwise no message would ever be delivered.");

        RuleFor(x => x.LeaseDuration)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Outbox lease duration must be greater than zero, otherwise a claim would expire the moment it is taken.");

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
