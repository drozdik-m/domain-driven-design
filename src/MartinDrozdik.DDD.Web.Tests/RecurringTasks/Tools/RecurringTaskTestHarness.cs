using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks.Tools;

/// <summary>
/// Wires a <see cref="RecurringTaskHost{TTask}"/> to:
/// <list type="bullet">
///     <item>a fake clock</item>
///     <item>a recording task</item>
///     <item>a recording logger</item>
/// </list>
/// so a test can drive the loop tick by tick.
/// </summary>
internal sealed class RecurringTaskTestHarness : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly RecurringTaskHost<TestRecurringTask> _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTaskTestHarness"/> class.
    /// </summary>
    /// <param name="options">The schedule the host runs on.</param>
    public RecurringTaskTestHarness(RecurringTaskOptions<TestRecurringTask> options)
    {
        Task = new TestRecurringTask();

        // Scoped as the real registration is, but always the same instance so the test can observe it
        var services = new ServiceCollection();
        services.AddScoped(_ => Task);
        _provider = services.BuildServiceProvider();

        _host = new RecurringTaskHost<TestRecurringTask>(
            new OptionsWrapper<RecurringTaskOptions<TestRecurringTask>>(options),
            Trigger,
            _provider.GetRequiredService<IServiceScopeFactory>(),
            Time,
            Logger.For<RecurringTaskHost<TestRecurringTask>>());
    }

    /// <summary>
    /// Gets the fake clock every delay in the host is measured against.
    /// </summary>
    public ObservableFakeTimeProvider Time { get; } = new();

    /// <summary>
    /// Gets everything the host logged.
    /// </summary>
    public TestLogger Logger { get; } = new();

    /// <summary>
    /// Gets the on-demand trigger the host listens to.
    /// </summary>
    public RecurringTaskTrigger<TestRecurringTask> Trigger { get; } = new();

    /// <summary>
    /// Gets the task the host runs.
    /// </summary>
    public TestRecurringTask Task { get; }

    /// <summary>
    /// Gets the loop, available once the host has been started.
    /// </summary>
    public Task? ExecuteTask => _host.ExecuteTask;

    /// <summary>
    /// Starts the loop.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the test gives up waiting.</param>
    /// <returns>A <see cref="Task"/> that completes once the loop has been started.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _host.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the loop and waits for it to finish, as the host does on shutdown.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the test gives up waiting.</param>
    /// <returns>A <see cref="Task"/> that completes once the loop is over.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _host.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _host.Dispose();
        _provider.Dispose();
    }
}
