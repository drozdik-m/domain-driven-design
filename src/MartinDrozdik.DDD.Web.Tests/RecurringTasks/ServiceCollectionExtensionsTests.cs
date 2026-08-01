using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies what <see cref="Web.RecurringTasks.ServiceCollectionExtensions.RemoveRecurringTasks(IServiceCollection)"/>
/// takes away, and what it leaves behind.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RemoveRecurringTasks_removes_the_loop_but_keeps_the_task_resolvable()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Act
        builder.Services.RemoveRecurringTasks();

        // Assert
        using var host = builder.Build();
        Assert.Empty(host.Services.GetServices<IHostedService>().OfType<RecurringTaskHost<ProbeTask>>());

        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProbeTask>());
    }

    [Fact]
    public void RemoveRecurringTasks_leaves_other_hosted_services_alone()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));
        builder.Services.AddHostedService<UnrelatedHostedService>();

        // Act
        builder.Services.RemoveRecurringTasks();

        // Assert
        using var host = builder.Build();
        Assert.Single(host.Services.GetServices<IHostedService>().OfType<UnrelatedHostedService>());
    }

    [Fact]
    public void RemoveRecurringTasks_removes_every_loop_not_just_the_first()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddRecurringTask<ProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));
        builder.AddRecurringTask<OtherProbeTask>(options => options.Period = TimeSpan.FromMinutes(1));

        // Act
        builder.Services.RemoveRecurringTasks();

        // Assert
        using var host = builder.Build();
        Assert.Empty(host.Services.GetServices<IHostedService>());
    }

    [Fact]
    public void RemoveRecurringTasks_on_a_collection_without_any_is_harmless()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.Services.AddHostedService<UnrelatedHostedService>();

        // Act
        builder.Services.RemoveRecurringTasks();

        // Assert
        using var host = builder.Build();
        Assert.Single(host.Services.GetServices<IHostedService>().OfType<UnrelatedHostedService>());
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        return Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
    }

    private sealed class ProbeTask : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OtherProbeTask : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnrelatedHostedService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
