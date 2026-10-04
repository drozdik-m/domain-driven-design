using System.Collections.Concurrent;
using System.Threading.Channels;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies the loop exactly as <see cref="Web.RecurringTasks.HostApplicationBuilderExtensions.AddRecurringTask{TTask}(IHostApplicationBuilder, Action{RecurringTaskOptions{TTask}})"/>
/// wires it into a real host, which the harness-driven <see cref="RecurringTaskHostTests"/> bypass.
/// </summary>
/// <remarks>
/// The host still runs on a <see cref="ObservableFakeTimeProvider"/>, registered before the task so that the
/// <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection, TService)"/> of <see cref="TimeProvider.System"/> leaves it in place.
/// </remarks>
public class RecurringTaskHostedLoopTests
{
    private static readonly TimeSpan s_period = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Never registered, so <see cref="UnresolvableTask"/> cannot be constructed.
    /// </summary>
    private interface IMissingDependency
    {
    }

    [Fact]
    public async Task Every_iteration_gets_a_fresh_scope_that_is_disposed_when_it_ends()
    {
        // Arrange
        var time = new ObservableFakeTimeProvider();
        var builder = CreateBuilder(time);
        builder.Services.AddSingleton<ScopeLog>();
        builder.Services.AddScoped<ScopedDependency>();
        builder.AddRecurringTask<ScopedTask>(options => options.Period = s_period);
        using var host = builder.Build();
        var log = host.Services.GetRequiredService<ScopeLog>();

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);
        await log.WaitForRunAsync(TestContext.Current.CancellationToken);
        await time.WaitForTimerAsync(1);
        time.Advance(s_period);
        await log.WaitForRunAsync(TestContext.Current.CancellationToken);
        await time.WaitForTimerAsync(2);
        await host.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        // Timer n is armed only after iteration n has left its scope, so both scopes are already gone
        Assert.Equal(2, log.Dependencies.Count);
        Assert.NotSame(log.Dependencies[0], log.Dependencies[1]);
        Assert.All(log.Dependencies, dependency => Assert.True(dependency.IsDisposed));
    }

    [Fact]
    public async Task Task_that_cannot_be_resolved_is_logged_and_tried_again_next_period()
    {
        // Arrange
        var time = new ObservableFakeTimeProvider();
        var logger = new TestLogger();
        var builder = CreateBuilder(time);
        builder.Logging.AddProvider(logger);
        builder.AddRecurringTask<UnresolvableTask>(options => options.Period = s_period);
        using var host = builder.Build();

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);
        await time.WaitForTimerAsync(1);
        time.Advance(s_period);
        await time.WaitForTimerAsync(2);
        await host.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        // A missing registration fails the iteration, never the loop - and never the application
        var failures = logger.At(LogLevel.Error)
            .Where(entry => entry.Message.Contains(nameof(UnresolvableTask), StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, failures.Count);
        Assert.All(failures, failure => Assert.IsType<InvalidOperationException>(failure.Exception));
    }

    private static HostApplicationBuilder CreateBuilder(TimeProvider time)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(time);
        return builder;
    }

    /// <summary>
    /// A scoped dependency remembering whether its scope disposed it.
    /// </summary>
    private sealed class ScopedDependency : IDisposable
    {
        private int _disposed;

        public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }

    /// <summary>
    /// Records the scoped dependency every iteration was given.
    /// </summary>
    private sealed class ScopeLog
    {
        private readonly ConcurrentQueue<ScopedDependency> _dependencies = new();
        private readonly Channel<int> _runs = Channel.CreateUnbounded<int>();

        public List<ScopedDependency> Dependencies => [.. _dependencies];

        public void Record(ScopedDependency dependency)
        {
            _dependencies.Enqueue(dependency);
            _runs.Writer.TryWrite(_dependencies.Count);
        }

        public async Task WaitForRunAsync(CancellationToken cancellationToken)
        {
            await _runs.Reader.ReadAsync(cancellationToken);
        }
    }

    private sealed class ScopedTask(ScopedDependency dependency, ScopeLog log) : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            log.Record(dependency);
            return Task.CompletedTask;
        }
    }

    private sealed class UnresolvableTask(IMissingDependency dependency) : IRecurringTask
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            GC.KeepAlive(dependency);
            return Task.CompletedTask;
        }
    }
}
