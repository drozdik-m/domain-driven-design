using MartinDrozdik.DDD.Extensions;
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
/// <param name="loggerFactory">Creates the logger of this task, under <see cref="LogCategory"/>.</param>
internal sealed class RecurringTaskHost<TTask>(
    IOptions<RecurringTaskOptions<TTask>> schedule,
    RecurringTaskTrigger<TTask> trigger,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : BackgroundService
    where TTask : class, IRecurringTask
{
    private static readonly string s_taskName = typeof(TTask).GetReadableName();

    private readonly ILogger _logger = loggerFactory.CreateLogger(LogCategory);

    /// <summary>
    /// Gets the logging category of this task's loop, e.g. <c>MartinDrozdik.DDD.Web.RecurringTasks.RecurringTaskHost.MyApp.Tasks.CleanupTask</c>.
    /// </summary>
    /// <remarks>
    /// Prevents multiple tasks from sharing the same category, which would make it impossible to filter them individually.
    /// </remarks>
    internal static string LogCategory => CreateLogCategory();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = schedule.Value;

        // Check for disabled tasks
        if (!options.Enabled)
        {
            RecurringTaskLogging.LogDisabled(_logger, s_taskName);
            return;
        }

        RecurringTaskLogging.LogScheduled(_logger, s_taskName, options.InitialDelay, options.Period);
        if (options.RunBetween is { } window)
        {
            RecurringTaskLogging.LogTimeWindow(_logger, s_taskName, window.From, window.To);
        }

        if (options.MaxRunsPerDay is { } maxRunsPerDay)
        {
            RecurringTaskLogging.LogDailyLimit(_logger, s_taskName, maxRunsPerDay);
        }

        var gate = new RecurringTaskRunGate(options.RunBetween, options.MaxRunsPerDay);

        // Run the loop until the application is shutting down
        try
        {
            var triggered = await WaitAsync(options.InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Outside the time window or over the daily limit, wait right until the next allowed moment.
                // A trigger raised meanwhile ends the wait early, and the check below simply parks the loop again.
                if (gate.GetWaitBeforeRun(timeProvider.GetLocalNow().DateTime) is { } wait)
                {
                    RecurringTaskLogging.LogRunPostponed(_logger, s_taskName, wait);
                    triggered = await WaitAsync(wait, stoppingToken);
                    continue;
                }

                gate.RecordRun(timeProvider.GetLocalNow().DateTime);
                await RunIterationAsync(options.Timeout, triggered, stoppingToken);
                triggered = await WaitAsync(options.Period, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down...
        }

        RecurringTaskLogging.LogStopping(_logger, s_taskName);
    }

    private static string CreateLogCategory()
    {
        var taskNamespace = typeof(TTask).Namespace;
        var prefix = $"{typeof(RecurringTaskHost<>).Namespace}.{nameof(RecurringTaskHost<>)}";
        return string.IsNullOrEmpty(taskNamespace)
            ? $"{prefix}.{s_taskName}"
            : $"{prefix}.{taskNamespace}.{s_taskName}";
    }

    /// <summary>
    /// Waits for the given delay, or until the task is triggered on demand — whichever happens first.
    /// </summary>
    /// <param name="delay">How long to wait. Non-positive means do not wait at all, only consume a pending trigger.</param>
    /// <param name="stoppingToken">Cancelled when the application is shutting down.</param>
    /// <returns><see langword="true"/> when the wait ended because of a trigger.</returns>
    private async Task<bool> WaitAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            // The next iteration starts right away, so it already serves any pending request
            return trigger.TryConsume();
        }

        // Delay via cancellation rather than a Task.Delay
        // * No orphan timers
        // * No Task.WhenAny
        // * No problems with a fake clock
        using var delayCts = new CancellationTokenSource(delay, timeProvider);
        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(delayCts.Token, stoppingToken);

        try
        {
            await trigger.WaitAsync(waitCts.Token);
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
        RecurringTaskLogging.LogIterationStarting(_logger, s_taskName, triggered);

        var startedAt = timeProvider.GetTimestamp();

        // Setup cancellation tokens for the iteration
        var limit = timeout.GetValueOrDefault();
        using var timeoutCts = timeout.HasValue
            ? new CancellationTokenSource(limit, timeProvider)
            : new CancellationTokenSource();
        using var iterationCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, stoppingToken);

        // Warn the moment the timeout elapses, so even a task that ignores its token and never returns gets reported
        await using var timeoutWarning = timeoutCts.Token.Register(() => RecurringTaskLogging.LogTimeoutElapsed(_logger, s_taskName, limit));

        try
        {
            // Execute the task in scoped DI
            await using var scope = scopeFactory.CreateAsyncScope();
            var task = scope.ServiceProvider.GetRequiredService<TTask>();
            await task.RunAsync(iterationCts.Token);

            // A task honouring its timeout by returning early did not finish its work
            if (timeoutCts.IsCancellationRequested)
            {
                RecurringTaskLogging.LogIterationEndedAfterTimeout(_logger, s_taskName, limit, Elapsed(startedAt));
            }
            else
            {
                RecurringTaskLogging.LogIterationCompleted(_logger, s_taskName, Elapsed(startedAt));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down... Let ExecuteAsync end the loop.
            throw;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            RecurringTaskLogging.LogIterationEndedAfterTimeout(_logger, s_taskName, limit, Elapsed(startedAt));
        }
        catch (Exception exception) when (stoppingToken.IsCancellationRequested)
        {
            // Drivers may report the shutdown cancellation as their own exception (SqlException, DbUpdateException, ...)
            RecurringTaskLogging.LogIterationFailedWhileStopping(_logger, exception, s_taskName, Elapsed(startedAt));
            stoppingToken.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            RecurringTaskLogging.LogIterationFailed(_logger, exception, s_taskName, Elapsed(startedAt));
        }
    }

    private double Elapsed(long startedAt)
    {
        return timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
    }
}
