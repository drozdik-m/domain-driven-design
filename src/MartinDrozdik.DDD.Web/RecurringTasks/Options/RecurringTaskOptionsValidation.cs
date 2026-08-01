using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.RecurringTasks.Options;

/// <summary>
/// Validates <see cref="RecurringTaskOptions{TTask}"/> with <see cref="RecurringTaskOptionsValidator{TTask}"/>.
/// </summary>
/// <typeparam name="TTask">The task whose schedule is validated.</typeparam>
internal sealed class RecurringTaskOptionsValidation<TTask> : IValidateOptions<RecurringTaskOptions<TTask>>
    where TTask : IRecurringTask
{
    private static readonly RecurringTaskOptionsValidator<TTask> s_validator = new();

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RecurringTaskOptions<TTask> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = s_validator.Validate(options);
        if (!result.TryGetError(out var error))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = error.Details.Select(e => $"Failed options validation for recurring task {typeof(TTask).Name}.{e.Key} {e.Value}");
        return ValidateOptionsResult.Fail(failures);
    }
}
