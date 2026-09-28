using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

public class BlobMetadataConversionTests
{
    [Fact]
    public void Comparer_ignores_the_order_of_entries()
    {
        // Arrange
        var left = BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value;
        var right = BlobMetadata.Empty.With("height", "1080").Value.With("width", "1920").Value;

        // Act
        var equal = BlobMetadataConversion.Comparer.Equals(left, right);

        // Assert
        Assert.True(equal);
        Assert.Equal(BlobMetadataConversion.Comparer.GetHashCode(left), BlobMetadataConversion.Comparer.GetHashCode(right));
    }

    [Fact]
    public void Converter_round_trips_the_entries()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("page_count", "12").Value;

        // Act
        var json = (string)BlobMetadataConversion.Converter.ConvertToProvider(metadata)!;
        var restored = (BlobMetadata)BlobMetadataConversion.Converter.ConvertFromProvider(json)!;

        // Assert
        Assert.Equal(metadata, restored);
    }
}
