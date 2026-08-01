using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Hosted loop that runs a <typeparamref name="TTask"/> on a schedule, and on demand whenever <see cref="IRecurringTaskTrigger{TTask}.Trigger"/> is called.
/// </summary>
/// <typeparam name="TTask">The task to run.</typeparam>
/// <param name="schedule">The schedule of this task.</param>
/// <param name="trigger">The on-demand trigger shared with the rest of the application.</param>
/// <param name="scopeFactory">Creates a dependency injection scope per iteration.</param>
/// <param name="timeProvider">Drives every delay, so tests can use a fake clock.</param>
/// <param name="logger">Target logger.</param>
internal sealed class RecurringTaskHost<TTask>(
    IOptions<RecurringTaskOptions<TTask>> schedule,
    RecurringTaskTrigger<TTask> trigger,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RecurringTaskHost<TTask>> logger) : BackgroundService
    where TTask : class, IRecurringTask
{
    private static readonly string s_taskName = typeof(TTask).Name;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = schedule.Value;

        // Check for disabled tasks
        if (!options.Enabled)
        {
            RecurringTaskLogging.LogDisabled(logger, s_taskName);
            return;
        }

        RecurringTaskLogging.LogScheduled(logger, s_taskName, options.InitialDelay, options.Period);

        // Run the loop until the application is shutting down
        try
        {
            var triggered = await WaitAsync(options.InitialDelay, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunIterationAsync(options.Timeout, triggered, stoppingToken).ConfigureAwait(false);
                triggered = await WaitAsync(options.Period, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down...
        }

        RecurringTaskLogging.LogStopping(logger, s_taskName);
    }

    /// <summary>
    /// Waits for the given delay, or until the task is triggered on demand — whichever happens first.
    /// </summary>
    /// <param name="delay">How long to wait. Non-positive means do not wait at all.</param>
    /// <param name="stoppingToken">Cancelled when the application is shutting down.</param>
    /// <returns><see langword="true"/> when the wait ended because of a trigger.</returns>
    private async Task<bool> WaitAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return false;
        }

        // Delay via cancellation rather than a Task.Delay
        // * No orphan timers
        // * No Task.WhenAny
        // * No problems with a fake clock
        using var delayCts = new CancellationTokenSource(delay, timeProvider);
        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(delayCts.Token, stoppingToken);

        try
        {
            await trigger.WaitAsync(waitCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // The delay elapsed without anybody triggering. That is the ordinary scheduled path.
            return false;
        }
    }

    /// <summary>
    /// Runs a single iteration in its own dependency injection scope, absorbing any failure so that one bad run never takes the loop down with it.
    /// </summary>
    /// <param name="timeout">How long this iteration may run, or <see langword="null"/> for no limit.</param>
    /// <param name="triggered">Whether this iteration was requested on demand.</param>
    /// <param name="stoppingToken">Cancelled when the application is shutting down.</param>
    /// <returns>A <see cref="Task"/> that completes when the iteration is over.</returns>
    private async Task RunIterationAsync(TimeSpan? timeout, bool triggered, CancellationToken stoppingToken)
    {
        RecurringTaskLogging.LogIterationStarting(logger, s_taskName, triggered);

        var startedAt = timeProvider.GetTimestamp();

        // Setup cancellation tokens for the iteration
        using var timeoutCts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value, timeProvider)
            : new CancellationTokenSource();
        using var iterationCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, stoppingToken);

        try
        {
            // Execute the task in scoped DI
            await using var scope = scopeFactory.CreateAsyncScope();
            var task = scope.ServiceProvider.GetRequiredService<TTask>();
            await task.RunAsync(iterationCts.Token).ConfigureAwait(false);

            RecurringTaskLogging.LogIterationCompleted(logger, s_taskName, Elapsed(startedAt));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down... Let ExecuteAsync end the loop.
            throw;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            var timeoutLog = timeout ?? TimeSpan.Zero; // Should never be null here
            RecurringTaskLogging.LogIterationTimedOut(logger, s_taskName, timeoutLog, Elapsed(startedAt));
        }
        catch (Exception exception)
        {
            RecurringTaskLogging.LogIterationFailed(logger, exception, s_taskName, Elapsed(startedAt));
        }
    }

    private double Elapsed(long startedAt)
    {
        return timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
    }
}
