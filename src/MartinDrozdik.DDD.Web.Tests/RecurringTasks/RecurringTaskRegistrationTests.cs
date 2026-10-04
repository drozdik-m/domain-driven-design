using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public void Task_already_registered_as_a_singleton_is_rejected()
    {
        // Arrange
        // E.g. assembly scanning that registers every service as a singleton
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ProbeTask>();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1)));

        // Assert
        Assert.Contains("singleton", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(RecurringTaskRegistrationTests) + "." + nameof(ProbeTask), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Task_already_registered_as_transient_is_kept_and_created_per_iteration()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.Services.AddTransient<ProbeTask>();

        // Act
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Assert
        var registration = Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(ProbeTask));
        Assert.Equal(ServiceLifetime.Transient, registration.Lifetime);
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        Assert.NotSame(
            scope.ServiceProvider.GetRequiredService<ProbeTask>(),
            scope.ServiceProvider.GetRequiredService<ProbeTask>());
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
    public void Registering_a_task_twice_applies_both_configurations_in_order()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        // The second call does not replace the first schedule, it refines it
        builder.AddRecurringTask<ProbeTask>(options =>
        {
            options.Enabled = false;
            options.Period = TimeSpan.FromMinutes(1);
        });
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(2));

        // Assert
        using var host = builder.Build();
        var schedule = Schedule<ProbeTask>(host);
        Assert.False(schedule.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(2), schedule.Period);
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
    public async Task Logging_of_one_task_can_be_filtered_without_silencing_the_others()
    {
        // Arrange
        var logger = new TestLogger();
        var builder = CreateBuilder();
        builder.Logging.AddProvider(logger);
        builder.Logging.AddFilter(
            "MartinDrozdik.DDD.Web.RecurringTasks.RecurringTaskHost.MartinDrozdik.DDD.Web.Tests.RecurringTasks.RecurringTaskRegistrationTests.ProbeTask",
            LogLevel.None);
        builder.AddRecurringTask<ProbeTask>(Disabled);
        builder.AddRecurringTask<Duplicate.ProbeTask>(Disabled);
        using var host = builder.Build();

        // Act
        // A disabled loop logs that it is disabled and ends. ExecuteAsync runs on a background thread,
        // so the test awaits both loops rather than assuming they logged by the time StartAsync returned.
        await host.StartAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(host.Services.GetServices<IHostedService>()
            .OfType<BackgroundService>()
            .Select(loop => loop.ExecuteTask ?? Task.CompletedTask));
        await host.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        var disabled = logger.Entries.FindAll(entry => entry.Message.Contains("is disabled", StringComparison.Ordinal));
        var entry = Assert.Single(disabled);
        Assert.Contains("Duplicate.ProbeTask", entry.Category, StringComparison.Ordinal);

        static void Disabled<TTask>(RecurringTaskOptions<TTask> options)
            where TTask : IRecurringTask
        {
            options.Enabled = false;
            options.Period = TimeSpan.FromMinutes(1);
        }
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

    [Fact]
    public async Task Schedule_longer_than_a_timer_can_wait_fails_the_application_at_startup()
    {
        // Arrange
        // Every two months. Without an upper bound this starts fine, runs once, and then the period wait throws
        // ArgumentOutOfRangeException, which BackgroundServiceExceptionBehavior.StopHost turns into the whole application stopping.
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromDays(60));
        using var host = builder.Build();

        // Act
        var exception = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));

        // Assert
        var validation = Assert.IsType<OptionsValidationException>(exception);
        Assert.Contains(
            validation.Failures,
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
