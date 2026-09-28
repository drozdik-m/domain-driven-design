using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Options;
using MartinDrozdik.DDD.Web.Blobs.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Runs the store contract against a real folder on disk.
/// </summary>
/// <remarks>
/// The same suite that guards <see cref="InMemoryBlobStore"/>, so the two are provably interchangeable and a
/// test written against the in-memory one still says something true about production. The tests declared here
/// are about the file layout, which the contract deliberately says nothing about.
/// </remarks>
public sealed class FileBlobStoreTests : BlobStoreContractTests, IDisposable
{
    private static readonly BlobContainer s_otherContainer = "avatars";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_blobs");

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A test that leaves a file open should not fail the run over a temp folder
        }
    }

    [Fact]
    public async Task A_download_in_progress_does_not_stop_a_delete()
    {
        // Arrange
        // A blob downloaded often enough would otherwise never expire on Windows
        var store = CreateStore();
        var key = CreateKey();
        await WriteAsync(store, key, "hello");
        await using var download = await store.OpenReadAsync(key, TestContext.Current.CancellationToken);

        // Act
        var deleted = await store.DeleteAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(deleted);
        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
        using var reader = new StreamReader(download!);
        Assert.Equal("hello", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_blob_is_a_file_named_by_its_id_inside_the_folder_of_its_container()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey();

        // Act
        await WriteAsync(store, key, "hello");

        // Assert
        Assert.True(File.Exists(Path.Combine(_root, key.Container.Name, key.Id.ToString())));
    }

    [Fact]
    public async Task Every_container_is_kept_in_its_own_configured_folder()
    {
        // Arrange
        // The folder is exactly where the files land - nothing named after the container is added to it
        var store = CreateStore();
        var key = CreateKey(s_otherContainer);

        // Act
        await WriteAsync(store, key, "hello");

        // Assert
        Assert.True(File.Exists(Path.Combine(_root, "elsewhere", key.Id.ToString())));
    }

    [Fact]
    public Task A_container_without_a_folder_is_refused()
    {
        // Arrange
        var store = CreateStore();
        var key = CreateKey("unknown");

        // Act
        var exists = () => store.ExistsAsync(key, TestContext.Current.CancellationToken);

        // Assert
        return Assert.ThrowsAsync<BlobException>(exists);
    }

    [Fact]
    public async Task Deleting_a_blob_keeps_its_container_folder()
    {
        // Arrange
        // Removing it would race a write creating a file in it on another instance
        var store = CreateStore();
        var key = CreateKey();
        await WriteAsync(store, key, "hello");

        // Act
        await store.DeleteAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(Directory.Exists(Path.Combine(_root, key.Container.Name)));
    }

    /// <inheritdoc />
    protected override IBlobStore CreateStore(TimeProvider timeProvider)
    {
        Directory.CreateDirectory(_root);

        var fileOptions = new FileBlobOptions();
        fileOptions.Containers.Add("invoices", new FileBlobContainerOptions { Path = Path.Combine(_root, "invoices") });
        fileOptions.Containers.Add(s_otherContainer, new FileBlobContainerOptions { Path = Path.Combine(_root, "elsewhere") });
        var options = Microsoft.Extensions.Options.Options.Create(fileOptions);

        return new FileBlobStore(options, timeProvider, NullLogger<FileBlobStore>.Instance);
    }
}
