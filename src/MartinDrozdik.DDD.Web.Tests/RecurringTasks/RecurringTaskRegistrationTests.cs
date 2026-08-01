using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies what <see cref="Web.RecurringTasks.HostApplicationBuilderExtensions.AddRecurringTask{TTask}(IHostApplicationBuilder, Action{RecurringTaskOptions{TTask}})"/>
/// registers.
/// </summary>
public class RecurringTaskRegistrationTests
{
    [Fact]
    public void Registered_task_is_scoped_so_it_can_inject_scoped_services()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Act
        using var host = builder.Build();
        using var firstScope = host.Services.CreateScope();
        using var secondScope = host.Services.CreateScope();

        // Assert
        Assert.NotSame(
            firstScope.ServiceProvider.GetRequiredService<ProbeTask>(),
            secondScope.ServiceProvider.GetRequiredService<ProbeTask>());
    }

    [Fact]
    public void Trigger_is_registered_as_a_singleton_so_producers_and_the_loop_share_it()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Act
        using var host = builder.Build();
        var trigger = host.Services.GetRequiredService<IRecurringTaskTrigger<ProbeTask>>();

        // Assert
        Assert.Same(trigger, host.Services.GetRequiredService<IRecurringTaskTrigger<ProbeTask>>());
    }

    [Fact]
    public void Registering_a_task_adds_exactly_one_loop()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Assert
        using var host = builder.Build();
        Assert.Single(host.Services.GetServices<IHostedService>().OfType<RecurringTaskHost<ProbeTask>>());
    }

    [Fact]
    public void Schedules_of_two_tasks_do_not_bleed_into_each_other()
    {
        // Arrange
        // Both tasks are called ProbeTask, so a schedule keyed by the short type name would silently merge the two
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));
        builder.AddRecurringTask<Duplicate.ProbeTask>(options => options.Period = TimeSpan.FromHours(3));

        // Act
        using var host = builder.Build();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), Schedule<ProbeTask>(host).Period);
        Assert.Equal(TimeSpan.FromHours(3), Schedule<Duplicate.ProbeTask>(host).Period);
    }

    [Fact]
    public async Task Invalid_schedule_fails_the_application_at_startup()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.Zero);
        using var host = builder.Build();

        // Act
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(RecurringTaskOptions<>.Period), StringComparison.Ordinal));
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        return Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
    }

    private static RecurringTaskOptions<TTask> Schedule<TTask>(IHost host)
        where TTask : class, IRecurringTask
    {
        return host.Services.GetRequiredService<IOptions<RecurringTaskOptions<TTask>>>().Value;
    }

    /// <summary>
    /// Holds a second task whose type name deliberately collides with the outer <c>ProbeTask</c>.
    /// </summary>
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members - the shadowing is what this fixture is for
    private static class Duplicate
    {
        internal sealed class ProbeTask : IRecurringTask
        {
            public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
#pragma warning restore S3218

    private sealed class ProbeTask : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
