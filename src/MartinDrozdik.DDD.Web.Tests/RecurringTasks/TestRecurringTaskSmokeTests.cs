using MartinDrozdik.DDD.Testing.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

public class TestRecurringTaskSmokeTests(ITestOutputHelper testOutputHelper)
    : RecurringTaskSmokeTests<Program, TestRecurringTask>(new TestedWebAppBuilder(testOutputHelper))
{
    [Fact]
    public async Task Smoke_tests_never_request_a_run_of_the_task()
    {
        // Arrange
        // The loops are removed, so a request would sit in the trigger instead of being consumed
        var trigger = Assert.IsType<RecurringTaskTrigger<TestRecurringTask>>(
            App.Services.GetRequiredService<IRecurringTaskTrigger<TestRecurringTask>>());

        // Act
        Task_has_a_registered_trigger();
        Task_schedule_is_valid();
        Task_resolves_with_all_its_dependencies();

        // Assert
        // A smoke test runs against whatever builder the consumer passes in.
        // With .WithRecurringTasks() on it, a request would execute the real job - sending mail, mutating data - which is what these tests promise never to do.
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await trigger.WaitAsync(timeout.Token));
    }
}
