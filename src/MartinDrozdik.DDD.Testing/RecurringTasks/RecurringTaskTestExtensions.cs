using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Testing.RecurringTasks;

/// <summary>
/// Extensions for testing <see cref="IRecurringTask"/> implementations without waiting for their schedule.
/// </summary>
public static class RecurringTaskTestExtensions
{
    /// <summary>
    /// Runs a single iteration of a recurring task, in a fresh dependency injection scope, exactly as the real loop would.
    /// </summary>
    /// <remarks>
    /// This is a direct invocation, not the loop. There is no waiting for schedule, error handling, or logging. It is meant for testing the task itself, not the loop.
    /// </remarks>
    /// <typeparam name="TTask">The recurring task to run.</typeparam>
    /// <param name="services">The service provider of the application.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The running iteration.</returns>
    public static async Task RunRecurringTaskAsync<TTask>(this IServiceProvider services, CancellationToken cancellationToken)
        where TTask : class, IRecurringTask
    {
        ArgumentNullException.ThrowIfNull(services);

        // Run the task in scope
        await using var scope = services.CreateAsyncScope();
        var task = scope.ServiceProvider.GetRequiredService<TTask>();
        await task.RunAsync(cancellationToken);
    }

    /// <inheritdoc cref="RunRecurringTaskAsync{TTask}(IServiceProvider, CancellationToken)"/>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    public static Task RunRecurringTaskAsync<TTask>(this ITestedApp testedApp, CancellationToken cancellationToken)
        where TTask : class, IRecurringTask
    {
        ArgumentNullException.ThrowIfNull(testedApp);
        return testedApp.Services.RunRecurringTaskAsync<TTask>(cancellationToken);
    }
}
