using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MartinDrozdik.DDD.Testing.RecurringTasks;

/// <summary>
/// Base class for smoke tests of a single recurring task, verifying that it is wired into the application correctly.
/// </summary>
/// <remarks>
/// These are wiring checks only.
/// To assert on what the job actually does, write a test calling <see cref="RecurringTaskTestExtensions.RunRecurringTaskAsync{TTask}(ITestedApp, CancellationToken)"/>.
/// </remarks>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
/// <typeparam name="TTask">The smoke tested recurring task.</typeparam>
public abstract class RecurringTaskSmokeTests<TProgram, TTask> : IDisposable
    where TProgram : class
    where TTask : class, IRecurringTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTaskSmokeTests{TProgram, TTask}"/> class.
    /// </summary>
    /// <param name="builder">App builder under test.</param>
    protected RecurringTaskSmokeTests(TestedAppBuilder<TProgram> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        App = builder.Build();
    }

    /// <summary>
    /// Gets the application under test, for derived classes adding tests of their own.
    /// </summary>
    protected TestedApp<TProgram> App { get; }

    /// <summary>
    /// Verifies the task has a trigger. It also confirms its registered.
    /// </summary>
    [Fact]
    public void Task_has_a_registered_trigger()
    {
        // Act
        var trigger = App.Services.GetService<IRecurringTaskTrigger<TTask>>();

        // Assert
        Assert.NotNull(trigger);
        trigger.Trigger();
    }

    /// <summary>
    /// Verifies the schedule of the task passes the validation of the application.
    /// </summary>
    [Fact]
    public void Task_schedule_is_valid()
    {
        // Arrange
        // Without a registered validation, resolving the schedule would check nothing and pass vacuously
        var validations = App.Services.GetServices<IValidateOptions<RecurringTaskOptions<TTask>>>();
        Assert.NotEmpty(validations);

        // Act
        // Resolving the schedule runs every registered validation, so an invalid one throws an OptionsValidationException right here
        var schedule = App.Services.GetRequiredService<IOptions<RecurringTaskOptions<TTask>>>().Value;

        // Assert
        App.TestOutputHelper.WriteLine($"{typeof(TTask).Name}: enabled={schedule.Enabled}, initial delay={schedule.InitialDelay}, period={schedule.Period}, timeout={schedule.Timeout?.ToString() ?? "none"}");
    }

    /// <summary>
    /// Verifies the task can be constructed.
    /// </summary>
    [Fact]
    public void Task_resolves_with_all_its_dependencies()
    {
        // Arrange
        using var scope = App.Services.CreateScope();

        // Act
        var task = scope.ServiceProvider.GetRequiredService<TTask>();

        // Assert
        Assert.NotNull(task);
    }

    /// <summary>
    /// Disposes the application under test.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            App.Dispose();
        }
    }
}
