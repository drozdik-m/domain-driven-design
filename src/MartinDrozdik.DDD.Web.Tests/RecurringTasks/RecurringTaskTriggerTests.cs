using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies the coalescing of <see cref="RecurringTaskTrigger{TTask}"/>, without a loop reading it concurrently.
/// </summary>
public class RecurringTaskTriggerTests
{
    [Fact]
    public void Many_triggers_leave_exactly_one_pending_request()
    {
        // Arrange
        var trigger = new RecurringTaskTrigger<TestRecurringTask>();

        // Act
        for (var i = 0; i < 50; i++)
        {
            trigger.Trigger();
        }

        // Assert
        Assert.True(trigger.TryConsume());
        Assert.False(trigger.TryConsume());
    }

    [Fact]
    public void Trigger_after_the_request_was_consumed_is_pending_again()
    {
        // Arrange
        var trigger = new RecurringTaskTrigger<TestRecurringTask>();
        trigger.Trigger();
        trigger.TryConsume();

        // Act
        trigger.Trigger();

        // Assert
        Assert.True(trigger.TryConsume());
    }

    [Fact]
    public async Task Pending_request_is_consumed_by_a_wait_without_blocking()
    {
        // Arrange
        var trigger = new RecurringTaskTrigger<TestRecurringTask>();
        trigger.Trigger();

        // Act
        var wait = trigger.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(wait.IsCompletedSuccessfully);
        await wait;
        Assert.False(trigger.TryConsume());
    }
}
