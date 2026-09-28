using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Exceptions;

namespace MartinDrozdik.DDD.Tests.Blobs.Models;

public class BlobTests
{
    private static readonly DateTime s_now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_keeps_who_stored_it_at_the_maximum_length()
    {
        // Arrange
        var createdBy = new string('a', Blob.CreatedByMaxLength);

        // Act
        var blob = CreateBlob(createdBy: createdBy);

        // Assert
        Assert.Equal(createdBy, blob.CreatedBy);
    }

    [Fact]
    public void Create_throws_when_who_stored_it_is_longer_than_the_maximum()
    {
        // Arrange
        var createdBy = new string('a', Blob.CreatedByMaxLength + 1);

        // Act
        // Assert
        // Truncating would silently record someone else
        Assert.Throws<BusinessRuleValidationException>(() => CreateBlob(createdBy: createdBy));
    }

    [Fact]
    public void Create_throws_for_a_negative_size()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<BusinessRuleValidationException>(() => CreateBlob(size: -1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_for_an_empty_original_file_name(string originalFileName)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<BusinessRuleValidationException>(() => CreateBlob(originalFileName: originalFileName));
    }

    [Fact]
    public void Create_shortens_an_original_file_name_longer_than_the_maximum()
    {
        // Arrange
        var originalFileName = new string('a', Blob.OriginalFileNameMaxLength + 10);

        // Act
        var blob = CreateBlob(originalFileName: originalFileName);

        // Assert
        // Anything is accepted as the name a file arrived with, so it is fitted to its column instead
        Assert.Equal(Blob.OriginalFileNameMaxLength, blob.OriginalFileName.Length);
    }

    /// <summary>
    /// Catalogues a blob with valid defaults for everything not given.
    /// </summary>
    /// <param name="originalFileName">The name it arrived with.</param>
    /// <param name="size">The number of bytes written.</param>
    /// <param name="createdBy">Who stored it, or null.</param>
    /// <returns>The new row.</returns>
    private static Blob CreateBlob(string originalFileName = "report.pdf", long size = 5, string? createdBy = null)
    {
        return Blob.Create(new CreateBlobParams
        {
            Key = BlobKey.Create("invoices", BlobId.New()),
            OriginalFileName = originalFileName,
            ContentType = MediaType.OctetStream,
            Size = size,
            CreatedAtUtc = s_now,
            Checksum = null,
            Metadata = BlobMetadata.Empty,
            CreatedBy = createdBy,
            ExpiresAtUtc = null,
        });
    }
}
