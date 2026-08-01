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
    /// Gets or sets a value indicating whether the task runs at all.
    /// Evaluated once at startup — a disabled task never starts its loop and cannot be triggered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how long to wait after application startup before the first iteration.
    /// Keeps background work from competing with the startup burst.
    /// A trigger raised during this delay starts the first iteration immediately.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the gap between iterations, measured from the moment the previous iteration <b>finished</b>.
    /// Iterations therefore never overlap and a slow iteration can never build up a backlog.
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets an optional limit on how long a single iteration may run.
    /// When it elapses, the <see cref="CancellationToken"/> passed to <see cref="IRecurringTask.RunAsync(CancellationToken)"/> is cancelled and the loop moves on.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
