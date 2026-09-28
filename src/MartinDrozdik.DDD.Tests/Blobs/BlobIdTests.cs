using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class BlobIdTests
{
    [Fact]
    public void New_creates_a_version_7_identity()
    {
        // Arrange
        // Act
        var id = BlobId.New();

        // Assert
        // Version 7 keeps the catalogue roughly in creation order without a second column
        Assert.Equal(7, id.Key.Version);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var key = Guid.CreateVersion7();
        BlobId id1 = key;
        BlobId id2 = key;
        var differentId = BlobId.New();

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: id1, id1, id2, differentId);
        EqualityAssert.TestEqualityOperators<ValueObject>(id1, id2, differentId);
    }
}
