using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Testing.Blobs;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Holds the in-memory store to the same contract as the real one, which is what makes it safe to test against.
/// </summary>
public sealed class InMemoryBlobStoreTests : BlobStoreContractTests
{
    /// <inheritdoc />
    protected override IBlobStore CreateStore(TimeProvider timeProvider) => new InMemoryBlobStore(timeProvider);
}
