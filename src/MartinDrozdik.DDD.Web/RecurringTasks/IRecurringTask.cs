using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// A unit of work executed repeatedly in the background by a hosted loop.
/// Register it with <see cref="HostApplicationBuilderExtensions.AddRecurringTask{TTask}(IHostApplicationBuilder, Action{RecurringTaskOptions{TTask}})"/>.
/// </summary>
/// <remarks>
/// Every iteration is resolved from a <b>fresh DI scope</b>,
/// so scoped services such as a <c>DbContext</c> can be injected through the constructor as usual.
/// </remarks>
public interface IRecurringTask
{
    /// <summary>
    /// Runs a single iteration of the task.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancelled when the application:
    /// <list type="bullet">
    ///     <item>is shutting down</item>
    ///     <item>exceeds <see cref="RecurringTaskOptions{TTask}.Timeout"/></item>
    /// </list>
    /// </param>
    /// <remarks>
    /// It is especially important to respect the <paramref name="cancellationToken"/> and exit promptly when it is cancelled.
    /// </remarks>
    /// <returns>
    /// A <see cref="Task"/> that completes when the iteration is done.
    /// </returns>
    Task RunAsync(CancellationToken cancellationToken);
}
