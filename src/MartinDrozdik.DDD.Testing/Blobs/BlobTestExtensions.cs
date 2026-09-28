using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MartinDrozdik.DDD.Testing.Blobs;

/// <summary>
/// Helpers for testing applications that store blobs.
/// </summary>
public static class BlobTestExtensions
{
    /// <summary>
    /// Replaces the application's blob store with one that keeps content in memory.
    /// Requires <see cref="TimeProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Faster than touching a disk and it leaves no garbage behind.
    /// </para>
    /// </remarks>
    /// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
    /// <param name="builder">The <see cref="TestedAppBuilder{TProgram}"/> to configure.</param>
    /// <returns>The builder, for chaining.</returns>
    public static TestedAppBuilder<TProgram> WithInMemoryBlobStore<TProgram>(this TestedAppBuilder<TProgram> builder)
        where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithServices(services =>
        {
            services.RemoveAll<IBlobStore>();
            services.AddSingleton<IBlobStore>(provider => new InMemoryBlobStore(provider.GetRequiredService<TimeProvider>()));
        });
    }

    /// <summary>
    /// Runs one blob sweep, instead of waiting for the schedule to come round.
    /// </summary>
    /// <param name="services">The services of the application under test.</param>
    /// <param name="cancellationToken">Cancelled when the test gives up.</param>
    /// <returns>What the sweep removed.</returns>
    public static async Task<BlobSweepResult> SweepBlobsAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        return await sweeper.SweepAsync(cancellationToken);
    }

    /// <summary>
    /// Runs one blob sweep, instead of waiting for the schedule to come round.
    /// </summary>
    /// <param name="app">The application under test.</param>
    /// <param name="cancellationToken">Cancelled when the test gives up.</param>
    /// <returns>What the sweep removed.</returns>
    public static Task<BlobSweepResult> SweepBlobsAsync(this ITestedApp app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.SweepBlobsAsync(cancellationToken);
    }
}
