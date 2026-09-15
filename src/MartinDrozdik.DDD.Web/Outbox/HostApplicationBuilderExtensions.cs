using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Interceptors;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the outbox engine over <typeparamref name="TDbContext"/>.
    /// The enqueue service and the registered handlers with their dispatchers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This adds no background loop. Pair it with
    /// <see cref="AddOutboxDispatchRecurringTask"/> for the built-in schedule, or leave it out and drive
    /// <see cref="IOutboxProcessor"/> from your own scheduler.
    /// </para>
    /// <para>
    /// It does not touch <typeparamref name="TDbContext"/> either. To have a commit wake the dispatch
    /// task rather than leaving the messages for the next poll, attach the registered
    /// <see cref="OutboxTaskTriggerInterceptor"/> to the context yourself.
    /// </para>
    /// <para>
    /// The table itself is mapped separately, by calling
    /// <see cref="ModelBuilderExtensions.AddOutbox(ModelBuilder, string, string?)"/> in the
    /// <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of <typeparamref name="TDbContext"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configureOptions">Action to configure the engine, or null to keep the defaults.</param>
    /// <param name="configure">Action registering the message types and their handlers.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// builder.AddOutbox&lt;InvoiceDbContext&gt;(
    ///     options =&gt; options.Retention = TimeSpan.FromDays(7),
    ///     config =&gt; config
    ///         .WithMessage&lt;SendEmailMessage, SendEmailHandler&gt;()
    ///         .WithMessage&lt;SendSmsMessage, SendSmsHandler&gt;());
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddOutbox<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<OutboxOptions>? configureOptions,
        Action<OutboxConfig> configure)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var optionsBuilder = builder.Services.AddOptions<OutboxOptions>();
        if (configureOptions is not null)
        {
            optionsBuilder.Configure(configureOptions);
        }

        optionsBuilder.ValidateOnStart();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidation>());

        // Normally already registered by AddAppServices, but this module stays usable on its own
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Build the registry
        var registry = new OutboxRegistry();
        builder.Services.TryAddSingleton(registry);

        builder.Services.TryAddScoped<IOutbox, Outbox<TDbContext>>();
        builder.Services.TryAddScoped<IOutboxProcessor, OutboxProcessor<TDbContext>>();

        // Registered whether or not the built-in dispatch task is used, so the interceptor always has something to trigger.
        // With no background service listening, it's a harmless no-op.
        builder.Services.TryAddSingleton<RecurringTaskTrigger<OutboxDispatchRecurringTask>>();
        builder.Services.TryAddSingleton<IRecurringTaskTrigger<OutboxDispatchRecurringTask>>(
            provider => provider.GetRequiredService<RecurringTaskTrigger<OutboxDispatchRecurringTask>>());

        // Only registered, never attached: the user must attach it to the context themselves
        builder.Services.TryAddScoped<OutboxTaskTriggerInterceptor>();

        configure(new OutboxConfig(builder.Services, registry));

        return builder;
    }

    /// <summary>
    /// Adds the built-in background service (task) that delivers outbox messages.
    /// </summary>
    /// <remarks>
    /// Skip this call entirely to schedule <see cref="IOutboxProcessor"/> yourself, from Quartz.NET or anything else.
    /// </remarks>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configure">Action to configure the schedule of the dispatch task.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// builder.AddOutboxDispatchTask(schedule =&gt;
    /// {
    ///     schedule.InitialDelay = TimeSpan.FromSeconds(10);
    ///     schedule.Period = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddOutboxDispatchRecurringTask(
        this IHostApplicationBuilder builder,
        Action<RecurringTaskOptions<OutboxDispatchRecurringTask>> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddRecurringTask(configure);
    }
}
