using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Blobs.Stores;
using MartinDrozdik.DDD.Web.Blobs.Sweepers;
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
    /// <b>Requires the outbox</b> - <c>AddOutbox&lt;TDbContext&gt;</c> with <c>WithBlobs()</c>.
    /// It's the only way to guarantee that the catalogue and the content are always in sync.
    /// </para>
    /// <para>
    /// <b>Every container is registered</b> with <see cref="BlobsConfig.WithContainer"/>.
    /// </para>
    /// <para>
    /// Do not forget to:
    /// </para>
    /// <list type="bullet">
    ///     <item><see cref="AddFileBlobStore"/>, or your own <see cref="IBlobStore"/> (where the content is stored),</item>
    ///     <item>map the blob DB table via <see cref="ModelBuilderExtensions.AddBlobs(ModelBuilder, string, string?)"/> in your <see cref="DbContext.OnModelCreating(ModelBuilder)"/>,</item>
    ///     <item>register sweeper of expired blobs (<see cref="AddBlobSweepRecurringTask"/>).</item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="config">Registers the containers and configures the sweep.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
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

        // Build the registry
        var registry = new BlobContainerRegistry();
        builder.Services.TryAddSingleton(registry);

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
}
