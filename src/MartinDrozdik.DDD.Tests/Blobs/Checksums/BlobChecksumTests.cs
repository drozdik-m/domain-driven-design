using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs.Checksums;

public class BlobChecksumTests
{
    private const string Sha256OfEmpty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void Sha256_normalizes_the_hash_to_lowercase()
    {
        // Arrange
        // Act
        var result = BlobChecksum.Sha256(Sha256OfEmpty.ToUpperInvariant());

        // Assert
        result.IsSuccess();
        Assert.Equal(Sha256OfEmpty, result.Value.Value);
    }

    [Fact]
    public void Sha256_rejects_a_hash_of_the_wrong_length()
    {
        // Arrange
        // Act
        var result = BlobChecksum.Sha256("abc123");

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
    }

    [Fact]
    public void Sha256_rejects_a_hash_that_is_not_hexadecimal()
    {
        // Arrange
        var value = new string('z', ChecksumAlgorithm.Sha256.HashLength);

        // Act
        var result = BlobChecksum.Sha256(value);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_returns_an_invalid_object_error_for_an_empty_hash(string? value)
    {
        // Arrange
        // Act
        var result = BlobChecksum.Create(ChecksumAlgorithm.Sha256, value);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
        Assert.Contains(result.Error.Details, detail => detail.Key == nameof(BlobChecksum.Value));
    }

    [Fact]
    public void ToETag_is_a_strong_quoted_entity_tag()
    {
        // Arrange
        var checksum = BlobChecksum.Sha256(Sha256OfEmpty).Value;

        // Act
        var etag = checksum.ToETag();

        // Assert
        // Strong, because blobs are immutable: the same content is always byte-for-byte the same
        Assert.Equal('"' + Sha256OfEmpty + '"', etag);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var checksum1 = BlobChecksum.Sha256(Sha256OfEmpty).Value;
        var checksum2 = BlobChecksum.Sha256(Sha256OfEmpty).Value;
        var differentChecksum = BlobChecksum.Sha256(new string('a', ChecksumAlgorithm.Sha256.HashLength)).Value;

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: checksum1, checksum1, checksum2, differentChecksum);
        EqualityAssert.TestEqualityOperators<ValueObject>(checksum1, checksum2, differentChecksum);
    }
}
