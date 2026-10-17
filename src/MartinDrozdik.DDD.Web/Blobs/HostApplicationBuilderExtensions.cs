using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Blobs.Stores;
using MartinDrozdik.DDD.Web.Blobs.Sweepers;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox.Registries;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.RecurringTasks.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds blob storage over <typeparamref name="TDbContext"/>: a catalogue in the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Requires the outbox of the same context</b> - <c>AddOutbox&lt;TDbContext&gt;</c> with <c>WithBlobs()</c>, called before this.
    /// It's the only way to guarantee that the catalogue and the content are always in sync:
    /// every removal of content is a message of that outbox, committed together with the catalogue.
    /// </para>
    /// <para>
    /// Blob storage is added once per application, over one context.
    /// </para>
    /// <para>
    /// <b>Every container is registered</b> with <see cref="BlobsConfig.WithContainer"/>.
    /// </para>
    /// <para>
    /// Do not forget to:
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="AddFileBlobStore"/>, or your own <see cref="IBlobStore"/> (where the content is stored),</item>
    ///     <item>map the blob DB table via <see cref="Blobs.ModelBuilderExtensions.AddBlobs(ModelBuilder)"/> or <see cref="Blobs.ModelBuilderExtensions.AddBlobs(ModelBuilder, TableName)"/> in your <see cref="DbContext.OnModelCreating(ModelBuilder)"/>,</item>
    ///     <item>register sweeper of expired blobs (<see cref="AddBlobSweepRecurringTask"/>).</item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="config">Registers the containers and configures the sweep.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="BlobException">
    /// The outbox of <typeparamref name="TDbContext"/> has not been added, it does not deliver the blob messages (<c>WithBlobs()</c>),
    /// or blob storage is already added.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.AddOutbox&lt;InvoiceDbContext&gt;(config =&gt; config.WithBlobs());
    /// builder.AddBlobs&lt;InvoiceDbContext&gt;(blobs =&gt; blobs
    ///     .WithContainer("invoice-scans", options =&gt; options.MaxSize = 20 * 1024 * 1024));
    /// builder.AddFileBlobStore(files =&gt; files
    ///     .WithContainer("invoice-scans", Path.Combine(builder.Environment.ContentRootPath, "blobs", "invoice-scans")));
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddBlobs<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<BlobsConfig> config)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(config);

        EnsureOutboxDeliversBlobs<TDbContext>(builder.Services);

        // The registry, options and storage exist once per application, so a second call would be silently dropped
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(BlobContainerRegistry)))
        {
            throw new BlobException($"Blob storage is already added, so it cannot be added over {typeof(TDbContext).GetReadableName()} too. Call AddBlobs once, registering every container in that call.");
        }

        // Build the registry
        var registry = new BlobContainerRegistry();
        builder.Services.AddSingleton(registry);

        builder.Services.AddOptions<BlobSweepOptions>().ValidateOnStart();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<BlobContainerOptions>, BlobContainerOptionsValidation>());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<BlobSweepOptions>, BlobSweepOptionsValidation>());

        config(new BlobsConfig(builder.Services, registry));

        // Normally already registered by AddAppServices, but this module stays usable on its own
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Scoped, because it works inside the caller's transaction
        builder.Services.TryAddScoped<IBlobStorage, BlobStorage<TDbContext>>();
        builder.Services.TryAddScoped<IBlobSweeper, BlobSweeper<TDbContext>>();

        return builder;
    }

    /// <summary>
    /// File-based blob storage: the content is stored in a folder, and the catalogue in the database.
    /// </summary>
    /// <remarks>
    /// Every container registered with <see cref="AddBlobs{TDbContext}"/> needs a folder, and every folder a registered container - either mismatch fails at startup.
    /// </remarks>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="config">Gives every container its folder.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// builder.AddFileBlobStore(files =&gt; files
    ///     .WithContainer("invoice-scans", Path.Combine(builder.Environment.ContentRootPath, "blobs", "invoice-scans")));
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddFileBlobStore(
        this IHostApplicationBuilder builder,
        Action<FileBlobStoreConfig> config)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(config);

        builder.Services.AddOptions<FileBlobOptions>().ValidateOnStart();
        config(new FileBlobStoreConfig(builder.Services));

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<FileBlobOptions>, FileBlobOptionsValidation>());

        // Normally already registered by AddAppServices, but this module stays usable on its own
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Stateless, so one instance serves every request
        builder.Services.TryAddSingleton<IBlobStore, FileBlobStore>();

        return builder;
    }

    /// <summary>
    /// Adds the background task that removes expired blobs.
    /// </summary>
    /// <remarks>
    /// Skip it to drive <see cref="IBlobSweeper"/> from your own scheduler instead.
    /// </remarks>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="configure">Action to configure the schedule of the sweep.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// builder.AddBlobSweepRecurringTask(schedule =&gt;
    /// {
    ///     schedule.InitialDelay = TimeSpan.FromMinutes(1);
    ///     schedule.Period = TimeSpan.FromHours(1);
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddBlobSweepRecurringTask(
        this IHostApplicationBuilder builder,
        Action<RecurringTaskOptions<BlobSweepRecurringTask>> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddRecurringTask(configure);
    }

    /// <summary>
    /// Makes sure the outbox of <typeparamref name="TDbContext"/> is added and delivers the blob messages,
    /// so every removal of content is committed together with the catalogue of that same context.
    /// </summary>
    /// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
    /// <param name="services">The services the outbox should have been added to.</param>
    /// <exception cref="BlobException">The outbox is missing, or it does not deliver <see cref="DeleteBlobMessage"/>.</exception>
    private static void EnsureOutboxDeliversBlobs<TDbContext>(IServiceCollection services)
        where TDbContext : DbContext
    {
        var contextName = typeof(TDbContext).GetReadableName();

        if (!OutboxRegistry<TDbContext>.TryFind(services, out var outbox))
        {
            throw new BlobException($"Blob storage over {contextName} requires the outbox of {contextName}, which delivers every removal of content. Call AddOutbox<{contextName}>(..., config => config.WithBlobs()) before AddBlobs<{contextName}>.");
        }

        if (!outbox.Contains(DeleteBlobMessage.MessageType))
        {
            throw new BlobException($"The outbox of {contextName} does not deliver {nameof(DeleteBlobMessage)}, which every removal of blob content rides on. Add WithBlobs() to the configuration of AddOutbox<{contextName}>.");
        }
    }
}
