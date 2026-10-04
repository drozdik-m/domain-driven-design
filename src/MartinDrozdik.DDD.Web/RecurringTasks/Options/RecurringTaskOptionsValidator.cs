using FluentValidation;

namespace MartinDrozdik.DDD.Web.RecurringTasks.Options;

/// <summary>
/// Validates the schedule of a recurring task.
/// </summary>
/// <typeparam name="TTask">The task the validated schedule belongs to.</typeparam>
internal sealed class RecurringTaskOptionsValidator<TTask> : AbstractValidator<RecurringTaskOptions<TTask>>
    where TTask : IRecurringTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTaskOptionsValidator{TTask}"/> class.
    /// </summary>
    public RecurringTaskOptionsValidator()
    {
        var maxWait = RecurringTaskOptions<TTask>.MaxWait;

        RuleFor(x => x.Period)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Recurring task period must be greater than zero, otherwise the task would spin without pause.")
            .LessThanOrEqualTo(maxWait)
            .WithMessage($"Recurring task period must not be longer than {maxWait.TotalDays} days. The schedule starts over with every restart, use a dedicated scheduler such as Quartz.NET for longer schedules.");

        RuleFor(x => x.InitialDelay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Recurring task initial delay must not be negative.")
            .LessThanOrEqualTo(maxWait)
            .WithMessage($"Recurring task initial delay must not be longer than {maxWait.TotalDays} days. The schedule starts over with every restart, use a dedicated scheduler such as Quartz.NET for longer schedules.");

        RuleFor(x => x.Timeout)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Recurring task timeout must be greater than zero when set.")
            .LessThanOrEqualTo(maxWait)
            .WithMessage($"Recurring task timeout must not be longer than {maxWait.TotalDays} days. Leave it unset for no limit.")
            .When(x => x.Timeout.HasValue);

        RuleFor(x => x.RunBetween)
            .Must(window => window!.From != window.To)
            .WithMessage("Recurring task time window must start and end at different times. Leave it unset to run all day.")
            .When(x => x.RunBetween is not null);

        RuleFor(x => x.MaxRunsPerDay)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Recurring task daily limit must be at least 1 when set. Leave it unset for no limit, or set Enabled to false to never run.")
            .When(x => x.MaxRunsPerDay.HasValue);
    }
}
