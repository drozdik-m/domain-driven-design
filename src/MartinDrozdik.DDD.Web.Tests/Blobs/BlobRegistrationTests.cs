using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Outbox;
using MartinDrozdik.DDD.Web.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Verifies how blob storage registers into a container, without a running application.
/// </summary>
public class BlobRegistrationTests
{
    private static readonly BlobContainer s_invoices = "invoices";
    private static readonly BlobContainer s_avatars = "avatars";

    [Fact]
    public void Blobs_over_the_context_of_the_outbox_resolve()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<CatalogueDbContext>(configureOptions: null, config => config.WithBlobs());

        // Act
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices));
        using var host = builder.Build();

        // Assert
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlobStorage>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlobSweeper>());
    }

    [Fact]
    public void Blobs_over_another_context_than_the_outbox_fail_to_resolve()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<OtherDbContext>(configureOptions: null, config => config.WithBlobs());

        // Act
        // The removals would be tracked in a context that the sweep never saves
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices));
        using var host = builder.Build();

        // Assert
        using var scope = host.Services.CreateScope();
        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IBlobStorage>());
        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IBlobSweeper>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_sweep_without_any_pass_fails_the_application_at_startup(int maxSweepPasses)
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<CatalogueDbContext>(configureOptions: null, config => config.WithBlobs());
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithSweep(options => options.MaxPasses = maxSweepPasses));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains(exception.Failures, failure => failure.Contains("sweep passes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Invalid_options_of_one_container_fail_the_application_at_startup_naming_it()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.AddOutbox<CatalogueDbContext>(configureOptions: null, config => config.WithBlobs());
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs
            .WithContainer(s_invoices)
            .WithContainer(s_avatars, options => options.MaxSize = 0));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        var failure = Assert.Single(exception.Failures);
        Assert.Contains("'avatars'", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void Registering_a_container_twice_fails()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var register = () => builder.AddBlobs<CatalogueDbContext>(blobs => blobs
            .WithContainer(s_invoices)
            .WithContainer(s_invoices));

        // Assert
        Assert.Throws<BlobException>(register);
    }

    [Fact]
    public void Giving_a_container_two_folders_fails()
    {
        // Arrange
        var builder = CreateBuilder();
        var root = CreateRoot();

        // Act
        var register = () => builder.AddFileBlobStore(files => files
            .WithContainer(s_invoices, Path.Combine(root, "a"))
            .WithContainer(s_invoices, Path.Combine(root, "b")));

        // Assert
        Assert.Throws<BlobException>(register);
    }

    [Fact]
    public async Task A_registered_container_without_a_folder_fails_the_application_at_startup()
    {
        // Arrange
        var root = CreateRoot();
        var builder = CreateFileStoreBuilder();
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices).WithContainer(s_avatars));
        builder.AddFileBlobStore(files => files.WithContainer(s_invoices, Path.Combine(root, "invoices")));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        var failure = Assert.Single(exception.Failures);
        Assert.Contains("'avatars' is registered but has no folder", failure, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_folder_of_a_container_nobody_registered_fails_the_application_at_startup()
    {
        // Arrange
        var root = CreateRoot();
        var builder = CreateFileStoreBuilder();
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices));
        builder.AddFileBlobStore(files => files
            .WithContainer(s_invoices, Path.Combine(root, "invoices"))
            .WithContainer(s_avatars, Path.Combine(root, "avatars")));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        var failure = Assert.Single(exception.Failures);
        Assert.Contains("'avatars' has a folder in the file store but is not registered", failure, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_container_configured_without_a_folder_fails_the_application_at_startup()
    {
        // Arrange
        var builder = CreateFileStoreBuilder();
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices));
        builder.AddFileBlobStore(files => files.WithContainer(s_invoices, _ => { }));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        var failure = Assert.Single(exception.Failures);
        Assert.Contains("'invoices' has an empty folder", failure, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("shared", "shared")]
    [InlineData("shared", "shared/")]
    [InlineData("shared", "SHARED")]
    [InlineData("outer", "outer/inner")]
    public async Task Containers_whose_folders_overlap_fail_the_application_at_startup(string invoicesFolder, string avatarsFolder)
    {
        // Arrange
        // Isolation: no container may see the files of another
        var root = CreateRoot();
        var builder = CreateFileStoreBuilder();
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices).WithContainer(s_avatars));
        builder.AddFileBlobStore(files => files
            .WithContainer(s_invoices, Path.Combine(root, invoicesFolder))
            .WithContainer(s_avatars, Path.Combine(root, avatarsFolder)));
        using var host = builder.Build();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
        var failure = Assert.Single(exception.Failures);
        Assert.Contains("overlap", failure, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Containers_in_sibling_folders_with_a_common_prefix_start()
    {
        // Arrange
        // "scans" and "scans-old" share a prefix, not a folder
        var root = CreateRoot();
        var builder = CreateFileStoreBuilder();
        builder.AddBlobs<CatalogueDbContext>(blobs => blobs.WithContainer(s_invoices).WithContainer(s_avatars));
        builder.AddFileBlobStore(files => files
            .WithContainer(s_invoices, Path.Combine(root, "scans"))
            .WithContainer(s_avatars, Path.Combine(root, "scans-old")));
        using var host = builder.Build();

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Builds a folder path of this test's own. Nothing is created in it - startup validation only compares paths.
    /// </summary>
    /// <returns>The path.</returns>
    private static string CreateRoot() => Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_blobs");

    /// <summary>
    /// Builds an application with the outbox, ready for blob storage over the real file store.
    /// </summary>
    /// <returns>The builder.</returns>
    private static HostApplicationBuilder CreateFileStoreBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<CatalogueDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.AddOutbox<CatalogueDbContext>(configureOptions: null, config => config.WithBlobs());
        return builder;
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddDbContext<CatalogueDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.Services.AddDbContext<OtherDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        builder.Services.AddSingleton<IBlobStore, InMemoryBlobStore>();
        return builder;
    }

    private sealed class CatalogueDbContext(DbContextOptions<CatalogueDbContext> options) : DbContext(options);

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options);
}
