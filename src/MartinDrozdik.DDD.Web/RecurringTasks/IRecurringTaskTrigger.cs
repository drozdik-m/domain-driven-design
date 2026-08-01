namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Wakes a recurring task up as soon as possible instead of waiting for its next scheduled run.
/// Inject it anywhere — a controller, a command handler, another background task.
/// </summary>
/// <typeparam name="TTask">The task to trigger.</typeparam>
/// <example>
/// <code>
/// public class InvoicesController(IRecurringTaskTrigger&lt;CleanupTask&gt; trigger) : ControllerBase
/// {
///     [HttpPost("cleanup")]
///     public IActionResult Cleanup()
///     {
///         trigger.Trigger();
///         return Accepted();
///     }
/// }
/// </code>
/// </example>
#pragma warning disable S2326 // TTask identifies which task is triggered
public interface IRecurringTaskTrigger<TTask>
#pragma warning restore S2326
    where TTask : IRecurringTask
{
    /// <summary>
    /// Requests an iteration as soon as possible. Returns immediately, non-blocking.
    /// </summary>
    /// <remarks>
    /// Requests are <b>coalesced</b>: while an iteration is pending or running, further calls collapse into the single pending request. Triggering a thousand times gets you one extra run, not a thousand.
    /// A request raised while an iteration is running is honoured <i>after</i> that iteration finishes.
    /// </remarks>
    void Trigger();
}
