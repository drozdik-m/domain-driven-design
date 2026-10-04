using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="IRecurringTask"/> that runs on the given schedule, and on demand through <see cref="IRecurringTaskTrigger{TTask}"/>.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TTask"/> is registered as scoped and resolved from a fresh scope for every iteration.
    /// A failing iteration is logged and the loop carries on.
    /// <para>
    /// An existing scoped or transient registration of <typeparamref name="TTask"/> is kept.
    /// </para>
    /// <para>
    /// Calling it again for the same <typeparamref name="TTask"/> adds no second loop, but every <paramref name="configure"/> is applied in order.
    /// </para>
    /// </remarks>
    /// <typeparam name="TTask">The task to run.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configure">Action to configure the schedule of <typeparamref name="TTask"/>.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="TTask"/> is already registered as a singleton.</exception>
    /// <example>
    /// <code>
    /// builder.AddRecurringTask&lt;CleanupTask&gt;(options =&gt;
    /// {
    ///     options.InitialDelay = TimeSpan.FromSeconds(30);
    ///     options.Period = TimeSpan.FromMinutes(10);
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddRecurringTask<TTask>(this IHostApplicationBuilder builder, Action<RecurringTaskOptions<TTask>> configure)
        where TTask : class, IRecurringTask
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddOptions<RecurringTaskOptions<TTask>>()
            .Configure(configure)
            .ValidateOnStart();

        // Singleton validator - added and implementation-deduped by TryAddEnumerable, so registering the same task twice validates it once
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RecurringTaskOptions<TTask>>, RecurringTaskOptionsValidation<TTask>>());

        // Normally already registered by AddAppServices, but this module stays usable on its own
        builder.Services.TryAddSingleton(TimeProvider.System);

        // The actual scoped task.
        // It's ok if transient or scoped is already registered, but not singleton.
        // Singleton would would share one instance outside its scope on every iteration - big no go.
        var singleton = builder.Services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == typeof(TTask)
            && !descriptor.IsKeyedService
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        if (singleton is not null)
        {
            throw new InvalidOperationException($"Recurring task {typeof(TTask).GetReadableName()} is already registered as a singleton. Every iteration resolves it from a fresh scope, so it must be scoped or transient.");
        }

        builder.Services.TryAddScoped<TTask>();

        // Register the concrete RecurringTaskTrigger<TTask> too, so the host can reach its internal WaitAsync
        builder.Services.TryAddSingleton<RecurringTaskTrigger<TTask>>();
        builder.Services.TryAddSingleton<IRecurringTaskTrigger<TTask>>(
            provider => provider.GetRequiredService<RecurringTaskTrigger<TTask>>());

        // AddHostedService deduplicates by implementation type, so registering a task twice still runs one loop. Both configure delegates apply tho.
        builder.Services.AddHostedService<RecurringTaskHost<TTask>>();

        return builder;
    }
}
