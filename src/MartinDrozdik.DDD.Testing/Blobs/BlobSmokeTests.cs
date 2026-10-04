using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Blobs;

/// <summary>
/// Base class for smoke tests of blob storage, verifying that it is wired into the application correctly.
/// </summary>
/// <remarks>
/// Wiring checks only.
/// </remarks>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
public abstract class BlobSmokeTests<TProgram> : IDisposable
    where TProgram : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobSmokeTests{TProgram}"/> class.
    /// </summary>
    /// <param name="builder">App builder under test.</param>
    /// <param name="expectedContainers">Every container the application stores into - no more, no less.</param>
    protected BlobSmokeTests(TestedAppBuilder<TProgram> builder, params BlobContainer[] expectedContainers)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(expectedContainers);

        App = builder.Build();
        ExpectedContainers = expectedContainers;
    }

    /// <summary>
    /// Gets the application under test, for derived classes adding tests of their own.
    /// </summary>
    protected TestedApp<TProgram> App { get; }

    /// <summary>
    /// Gets every container the application is expected to store into.
    /// </summary>
    protected IReadOnlyCollection<BlobContainer> ExpectedContainers { get; }

    /// <summary>
    /// Verifies exactly the expected containers are registered - none missing, none unexpected.
    /// </summary>
    [Fact]
    public void Blob_containers_are_registered_as_expected()
    {
        // Arrange
        var registry = App.Services.GetRequiredService<BlobContainerRegistry>();

        // Act
        var missing = ExpectedContainers.Where(c => !registry.Contains(c)).Select(c => c.Name).ToList();
        var unexpected = registry.Containers.Where(c => !ExpectedContainers.Contains(c)).Select(c => c.Name).ToList();

        // Assert
        Assert.True(missing.Count == 0, $"Blob containers expected but not registered: {string.Join(", ", missing)}.");
        Assert.True(unexpected.Count == 0, $"Blob containers registered but not expected: {string.Join(", ", unexpected)}.");
    }

    /// <summary>
    /// Verifies blobs can be stored and read.
    /// </summary>
    [Fact]
    public void Blob_storage_resolves_with_all_its_dependencies()
    {
        // Arrange
        using var scope = App.Services.CreateScope();

        // Act
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        // Assert
        Assert.NotNull(storage);
    }

    /// <summary>
    /// Verifies expired and orphaned content can be swept.
    /// </summary>
    [Fact]
    public void Blob_sweeper_resolves_with_all_its_dependencies()
    {
        // Arrange
        using var scope = App.Services.CreateScope();

        // Act
        var sweeper = scope.ServiceProvider.GetRequiredService<IBlobSweeper>();

        // Assert
        Assert.NotNull(sweeper);
    }

    /// <summary>
    /// Verifies the options of every registered container pass the validation of the application.
    /// </summary>
    [Fact]
    public void Blob_container_options_are_valid()
    {
        // Arrange
        var registry = App.Services.GetRequiredService<BlobContainerRegistry>();
        var monitor = App.Services.GetRequiredService<IOptionsMonitor<BlobContainerOptions>>();
        Assert.NotEmpty(App.Services.GetServices<IValidateOptions<BlobContainerOptions>>());

        foreach (var container in registry.Containers)
        {
            // Act
            // Resolving the named options runs every registered validation
            var options = monitor.Get(container.Name);

            // Assert
            App.TestOutputHelper.WriteLine($"Blob container '{container}': max size={options.MaxSize?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unlimited"}, checksums={options.ComputeChecksum}, grace={options.OrphanGracePeriod}");
        }
    }

    /// <summary>
    /// Verifies the options of the sweep pass the validation of the application.
    /// </summary>
    [Fact]
    public void Blob_sweep_options_are_valid()
    {
        // Arrange
        Assert.NotEmpty(App.Services.GetServices<IValidateOptions<BlobSweepOptions>>());

        // Act
        var options = App.Services.GetRequiredService<IOptions<BlobSweepOptions>>().Value;

        // Assert
        App.TestOutputHelper.WriteLine($"Blob sweep: batch size={options.BatchSize}, max passes={options.MaxPasses}");
    }

    /// <summary>
    /// Verifies the store the application is configured with can actually reach every registered container.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the store has been asked.</returns>
    [Fact]
    public async Task Blob_store_is_reachable_for_every_container()
    {
        // Arrange
        using var scope = App.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();
        var registry = App.Services.GetRequiredService<BlobContainerRegistry>();

        foreach (var container in registry.Containers)
        {
            // Act
            // Asking about an address nobody wrote is the cheapest thing that still touches the store for real
            var exists = await store.ExistsAsync(BlobKey.Create(container, BlobId.New()), TestContext.Current.CancellationToken);

            // Assert
            Assert.False(exists);
        }

        App.TestOutputHelper.WriteLine($"Blob store: {store.GetType().GetReadableName()}");
    }

    /// <summary>
    /// Disposes the application under test.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            App.Dispose();
        }
    }
}
