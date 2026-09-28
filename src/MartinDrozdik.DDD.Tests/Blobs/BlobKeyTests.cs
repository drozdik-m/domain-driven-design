using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class BlobKeyTests
{
    private static readonly BlobId s_id = new(Guid.Parse("0198b7c4-d2e8-7f10-8a3b-1c2d3e4f5a6b"));

    [Fact]
    public void Path_is_the_container_and_the_blob()
    {
        // Arrange
        var key = BlobKey.Create("invoices", s_id);

        // Act
        var path = key.Path;

        // Assert
        Assert.Equal($"invoices/{s_id}", path);
    }

    [Fact]
    public void Path_separates_with_forward_slashes_on_every_platform()
    {
        // Arrange
        var key = BlobKey.Create("invoices", s_id);

        // Act
        var path = key.Path;

        // Assert
        // A store translates to whatever its backend uses; the key itself stays portable
        Assert.DoesNotContain('\\', path);
        Assert.Equal(2, path.Split(BlobKey.Separator).Length);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var key1 = BlobKey.Create("invoices", s_id);
        var key2 = BlobKey.Create("invoices", s_id);
        var differentKey = BlobKey.Create("invoices", BlobId.New());

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: key1, key1, key2, differentKey);
        EqualityAssert.TestEqualityOperators<ValueObject>(key1, key2, differentKey);
    }
}
