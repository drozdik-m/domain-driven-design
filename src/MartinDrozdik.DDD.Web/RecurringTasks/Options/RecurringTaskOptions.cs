using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.RecurringTasks.Options;

/// <summary>
/// Schedule of <typeparamref name="TTask"/>, configured in code with <see cref="HostApplicationBuilderExtensions.AddRecurringTask{TTask}(IHostApplicationBuilder, Action{RecurringTaskOptions{TTask}})"/>.
/// </summary>
/// <remarks>
/// Mutable properties are used instead of init-only properties because the options are configured with an <see cref="Action{T}"/> rather than an object initializer.
/// </remarks>
/// <typeparam name="TTask">The task this schedule belongs to.</typeparam>
#pragma warning disable S2326 // Unused type parameters - used to mark the task relation
public sealed class RecurringTaskOptions<TTask>
#pragma warning restore S2326
    where TTask : IRecurringTask
{
    /// <summary>
    /// Gets the longest <see cref="InitialDelay"/>, <see cref="Period"/> and <see cref="Timeout"/> the schedule accepts: 45 days.
    /// </summary>
    /// <remarks>
    /// The schedule lives in memory and starts over with every restart, so a longer wait would rarely be reached anyway.
    /// It also keeps every wait safely below the roughly 49.7 days a timer-backed <see cref="CancellationTokenSource"/> can wait.
    /// Longer or persistent schedules belong in a dedicated scheduler such as Quartz.NET.
    /// </remarks>
    public static TimeSpan MaxWait => TimeSpan.FromDays(45);

    /// <summary>
    /// Gets or sets a value indicating whether the task runs at all.
    /// Evaluated once at startup — a disabled task never starts its loop and cannot be triggered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how long to wait after application startup before the first iteration.
    /// Keeps background work from competing with the startup burst.
    /// A trigger raised during this delay starts the first iteration immediately.
    /// Must not be negative or longer than <see cref="MaxWait"/>.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the gap between iterations, measured from the moment the previous iteration <b>finished</b>.
    /// Iterations therefore never overlap and a slow iteration can never build up a backlog.
    /// Must be greater than zero and not longer than <see cref="MaxWait"/>.
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets an optional limit on how long a single iteration may run.
    /// When it elapses, the <see cref="CancellationToken"/> passed to <see cref="IRecurringTask.RunAsync(CancellationToken)"/> is cancelled and a warning is logged.
    /// The task is not aborted: the loop continues once <see cref="IRecurringTask.RunAsync(CancellationToken)"/> returns, so iterations never overlap.
    /// When set, must be greater than zero and not longer than <see cref="MaxWait"/>; leave it <see langword="null"/> for no limit.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets an optional daily window of local time in which an iteration may start, e.g. a nightly window from 22:00 to 04:00.
    /// Checked just before each iteration against the local time of the <see cref="TimeProvider"/>.
    /// Outside the window the loop waits until <see cref="RecurringTaskTimeWindow.From"/>.
    /// Leave it <see langword="null"/> to run all day.
    /// </summary>
    /// <remarks>
    /// Checked against the local time of the <see cref="TimeProvider"/>.
    /// </remarks>
    public RecurringTaskTimeWindow? RunBetween { get; set; }

    /// <summary>
    /// Gets or sets an optional limit on how many iterations may start per day, e.g. 1 to run a task once a day.
    /// Every started iteration counts. Once the limit is reached the loop waits until the next day starts.
    /// A day starts at <see cref="RecurringTaskTimeWindow.From"/> of <see cref="RunBetween"/> when set.
    /// When set, must be at least 1; leave it <see langword="null"/> for no limit.
    /// </summary>
    /// <remarks>
    /// The runs are counted <b>in memory only</b>: the count starts over with every host restart.
    /// A guarantee that survives restarts or spans instances needs a dedicated scheduler such as Quartz.NET.
    /// </remarks>
    public int? MaxRunsPerDay { get; set; }
}
