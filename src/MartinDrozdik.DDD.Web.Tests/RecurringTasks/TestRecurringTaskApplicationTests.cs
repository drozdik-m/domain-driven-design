using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies that a recurring task registered by a real ASP.NET Core application actually runs, which the isolated host tests cannot prove on their own.
/// </summary>
public class TestRecurringTaskApplicationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Triggering_a_registered_task_runs_it_inside_the_running_application()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithRecurringTasks()
            .Build();
        var trigger = app.Services.GetRequiredService<IRecurringTaskTrigger<TestRecurringTask>>();
        var runs = app.Services.GetRequiredService<TestRecurringTaskRuns>();

        // Act
        trigger.Trigger();

        // Assert
        // The task is scheduled an hour out, so a run can only come from the trigger
        var run = await runs.WaitForRunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, run);
    }

    [Fact]
    public void Task_is_not_run_by_its_schedule_during_a_short_lived_test()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();

        // Act
        var runs = app.Services.GetRequiredService<TestRecurringTaskRuns>();

        // Assert
        Assert.Equal(0, runs.Count);
    }
}
