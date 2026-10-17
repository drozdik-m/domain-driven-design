using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox.Interceptors;
using MartinDrozdik.DDD.Web.Outbox.Options;
using MartinDrozdik.DDD.Web.Outbox.Registries;
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
    /// Adds the outbox of <typeparamref name="TDbContext"/>, with the default options.
    /// The enqueue service, the processor, and the registered handlers with their dispatchers.
    /// </summary>
    /// <remarks>
    /// See <see cref="AddOutbox{TDbContext}(IHostApplicationBuilder, Action{OutboxOptions{TDbContext}}, Action{OutboxConfig})"/>.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configure">Action registering the message types and their handlers.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="OutboxException">The outbox of <typeparamref name="TDbContext"/> is already added, or a message type cannot be registered.</exception>
    /// <example>
    /// <code>
    /// builder.AddOutbox&lt;ShippingDbContext&gt;(config =&gt; config.WithMessage&lt;SendEmailMessage, SendEmailHandler&gt;());
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddOutbox<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<OutboxConfig> configure)
        where TDbContext : DbContext
        => builder.AddOutbox<TDbContext>(static _ => { }, configure);

    /// <summary>
    /// Adds the outbox of <typeparamref name="TDbContext"/>.
    /// The enqueue service, the processor, and the registered handlers with their dispatchers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every context has an outbox of its own. Call this once per context. Register every message type of that context in the call.
    /// A message type may be registered with several outboxes, always with the same handler.
    /// </para>
    /// <para>
    /// This adds no background loop.
    /// Pair it with <see cref="AddOutboxDispatchRecurringTask{TDbContext}"/> for the built-in schedule,
    /// or leave it out and use <see cref="IOutboxProcessor{TDbContext}"/> from your own scheduler (Quartz.NET etc.).
    /// An application without either only enqueues messages.
    /// </para>
    /// <para>
    /// It does not touch <typeparamref name="TDbContext"/> either. To have a commit wake the dispatch
    /// task rather than leaving the messages for the next poll, attach the registered
    /// <see cref="OutboxTaskTriggerInterceptor{TDbContext}"/> to the context yourself.
    /// </para>
    /// <para>
    /// The table itself is mapped separately, by calling
    /// <see cref="ModelBuilderExtensions.AddOutbox(ModelBuilder, TableName)"/> in the
    /// <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of <typeparamref name="TDbContext"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox table.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configureOptions">Action to configure the outbox.</param>
    /// <param name="configure">Action registering the message types and their handlers.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="OutboxException">The outbox of <typeparamref name="TDbContext"/> is already added, or a message type cannot be registered.</exception>
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
        Action<OutboxOptions<TDbContext>> configureOptions,
        Action<OutboxConfig> configure)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(configure);

        // The registry, options and processor exist once per context, so a second call would be silently ignored
        if (OutboxRegistry<TDbContext>.TryFind(builder.Services, out _))
        {
            var contextName = typeof(TDbContext).GetReadableName();
            throw new OutboxException($"The outbox of {contextName} is already added. Call AddOutbox<{contextName}> once, registering every message type of that context in that call.");
        }

        builder.Services.AddOptions<OutboxOptions<TDbContext>>()
            .Configure(configureOptions)
            .ValidateOnStart();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OutboxOptions<TDbContext>>, OutboxOptionsValidation<TDbContext>>());

        // Normally already registered by AddAppServices, but this module stays usable on its own
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Build the registry
        var registry = new OutboxRegistry<TDbContext>();
        builder.Services.AddSingleton(registry);

        builder.Services.AddScoped<IOutbox<TDbContext>, Outbox<TDbContext>>();
        builder.Services.AddScoped<IOutboxProcessor<TDbContext>, OutboxProcessor<TDbContext>>();

        // Registered whether or not the built-in dispatch task is used, so the interceptor always has something to trigger.
        // With no background service listening, it's a harmless no-op.
        builder.Services.TryAddSingleton<RecurringTaskTrigger<OutboxDispatchRecurringTask<TDbContext>>>();
        builder.Services.TryAddSingleton<IRecurringTaskTrigger<OutboxDispatchRecurringTask<TDbContext>>>(
            provider => provider.GetRequiredService<RecurringTaskTrigger<OutboxDispatchRecurringTask<TDbContext>>>());

        // Only registered, never attached: the user must attach it to the context themselves
        builder.Services.AddScoped<OutboxTaskTriggerInterceptor<TDbContext>>();

        configure(new OutboxConfig(builder.Services, registry.Add));

        return builder;
    }

    /// <summary>
    /// Adds the built-in background service (task) that delivers the messages of the outbox of <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The outbox itself has to be added first, with <see cref="AddOutbox{TDbContext}(IHostApplicationBuilder, Action{OutboxOptions{TDbContext}}, Action{OutboxConfig})"/>.
    /// </para>
    /// <para>
    /// Skip the call to only enqueue messages in this application, or to schedule <see cref="IOutboxProcessor{TDbContext}"/> yourself, from Quartz.NET or anything else.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configure">Action to configure the schedule of the dispatch task.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="OutboxException">The outbox of <typeparamref name="TDbContext"/> has not been added.</exception>
    /// <example>
    /// <code>
    /// builder.AddOutboxDispatchRecurringTask&lt;InvoiceDbContext&gt;(schedule =&gt;
    /// {
    ///     schedule.InitialDelay = TimeSpan.FromSeconds(10);
    ///     schedule.Period = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddOutboxDispatchRecurringTask<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<RecurringTaskOptions<OutboxDispatchRecurringTask<TDbContext>>> configure)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!OutboxRegistry<TDbContext>.TryFind(builder.Services, out _))
        {
            var contextName = typeof(TDbContext).GetReadableName();
            throw new OutboxException($"The outbox of {contextName} has not been added, so there is nothing for its dispatch task to deliver. Call AddOutbox<{contextName}> before AddOutboxDispatchRecurringTask<{contextName}>.");
        }

        return builder.AddRecurringTask(configure);
    }
}
