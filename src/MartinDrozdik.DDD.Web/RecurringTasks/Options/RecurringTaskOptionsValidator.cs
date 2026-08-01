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
        RuleFor(x => x.Period)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Recurring task period must be greater than zero, otherwise the task would spin without pause.");

        RuleFor(x => x.InitialDelay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Recurring task initial delay must not be negative.");

        RuleFor(x => x.Timeout)
            .GreaterThan(TimeSpan.Zero)
            .When(x => x.Timeout.HasValue)
            .WithMessage("Recurring task timeout must be greater than zero when set.");
    }
}
