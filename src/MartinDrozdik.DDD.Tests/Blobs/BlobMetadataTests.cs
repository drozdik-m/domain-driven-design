using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class BlobMetadataTests
{
    [Fact]
    public void With_returns_a_copy_and_leaves_the_original_alone()
    {
        // Arrange
        var original = BlobMetadata.Empty;

        // Act
        var updated = original.With("width", "1920").Value;

        // Assert
        // Immutability is what lets the Entity Framework value comparer use an instance as its own snapshot
        Assert.Empty(original.Values);
        Assert.Equal("1920", updated["width"]);
    }

    [Fact]
    public void With_replaces_a_value_already_stored_under_the_key()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        var updated = metadata.With("width", "3840").Value;

        // Assert
        Assert.Equal("3840", updated["width"]);
        Assert.Single(updated.Values);
    }

    [Fact]
    public void Without_removes_a_key()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value;

        // Act
        var updated = metadata.Without("width");

        // Assert
        Assert.Null(updated["width"]);
        Assert.Equal("1080", updated["height"]);
    }

    [Fact]
    public void Without_a_key_that_is_not_there_changes_nothing()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        var updated = metadata.Without("height");

        // Assert
        Assert.Equal(metadata, updated);
    }

    [Fact]
    public void TryGetValue_reports_a_missing_key()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        var found = metadata.TryGetValue("height", out var value);

        // Assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void With_enforces_the_entry_limit_on_an_existing_instance()
    {
        // Arrange
        // The limits have to hold while growing, not only at construction - growing is the only way to reach them
        var metadata = BlobMetadata.Empty;
        for (var i = 0; i < BlobMetadata.MaxEntries; i++)
        {
            metadata = metadata.With($"key{i}", "value").Value;
        }

        // Act
        // Assert
        Assert.True(metadata.With("one-too-many", "value").IsFailure);
    }

    [Fact]
    public void With_enforces_the_total_length_limit()
    {
        // Arrange
        // Filled to just under the budget - the keys themselves count too, so one value short of the ceiling
        var metadata = BlobMetadata.Empty;
        var value = new string('a', BlobMetadata.MaxValueLength);
        for (var i = 0; i < (BlobMetadata.MaxTotalLength / BlobMetadata.MaxValueLength) - 1; i++)
        {
            metadata = metadata.With($"key{i}", value).Value;
        }

        // Act
        // Assert
        Assert.True(metadata.With("overflow", value).IsFailure);
    }

    [Fact]
    public void With_rejects_a_key_longer_than_the_maximum()
    {
        // Arrange
        var key = new string('a', BlobMetadata.MaxKeyLength + 1);

        // Act
        // Assert
        Assert.True(BlobMetadata.Empty.With(key, "value").IsFailure);
    }

    [Fact]
    public void With_rejects_a_value_longer_than_the_maximum()
    {
        // Arrange
        var value = new string('a', BlobMetadata.MaxValueLength + 1);

        // Act
        // Assert
        Assert.True(BlobMetadata.Empty.With("key", value).IsFailure);
    }

    [Theory]
    [InlineData("key with spaces")]
    [InlineData("key/with/slashes")]
    [InlineData("")]
    public void With_rejects_an_unsupported_key(string key)
    {
        // Arrange
        // Act
        // Assert
        Assert.True(BlobMetadata.Empty.With(key, "value").IsFailure);
    }

    [Fact]
    public void Equality_does_not_depend_on_the_order_entries_were_added()
    {
        // Arrange
        var metadata1 = BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value;
        var metadata2 = BlobMetadata.Empty.With("height", "1080").Value.With("width", "1920").Value;
        var differentMetadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        // Assert
        // The Entity Framework comparer rests on this, so a reordered dictionary is not a change worth saving
        EqualityAssert.TestEqualityComparer(comparer: metadata1, metadata1, metadata2, differentMetadata);
        EqualityAssert.TestEqualityOperators<ValueObject>(metadata1, metadata2, differentMetadata);
        Assert.Equal(metadata1.GetHashCode(), metadata2.GetHashCode());
    }

    [Fact]
    public void Keys_are_read_case_insensitively()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        var value = metadata["WIDTH"];

        // Assert
        Assert.Equal("1920", value);
        Assert.True(metadata.TryGetValue("Width", out _));
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("pageCount")]
    [InlineData("SCAN_STATE")]
    public void With_rejects_a_key_that_is_not_lowercase(string key)
    {
        // Arrange
        // Act
        var result = BlobMetadata.Empty.With(key, "value");

        // Assert
        // S3 lowercases keys and Azure ignores their case, so only lowercase reads back the same everywhere
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
        Assert.Contains("lowercase", Assert.Single(result.Error.Details).Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_rejects_a_key_that_is_not_lowercase()
    {
        // Arrange
        KeyValuePair<string, string>[] values = [new("Width", "1920")];

        // Act
        var result = BlobMetadata.Create(values);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
    }

    [Fact]
    public void Create_holds_the_given_entries()
    {
        // Arrange
        KeyValuePair<string, string>[] values = [new("width", "1920"), new("height", "1080")];

        // Act
        var result = BlobMetadata.Create(values);

        // Assert
        result.IsSuccess();
        Assert.Equal(BlobMetadata.Empty.With("width", "1920").Value.With("height", "1080").Value, result.Value);
    }

    [Fact]
    public void Create_rejects_a_key_given_twice()
    {
        // Arrange
        KeyValuePair<string, string>[] values = [new("width", "1920"), new("width", "3840")];

        // Act
        var result = BlobMetadata.Create(values);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
    }

    [Fact]
    public void Create_enforces_the_entry_limit()
    {
        // Arrange
        var values = Enumerable.Range(0, BlobMetadata.MaxEntries + 1)
            .Select(i => new KeyValuePair<string, string>($"key{i}", "value"));

        // Act
        var result = BlobMetadata.Create(values);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Without_removes_a_key_given_in_a_different_case()
    {
        // Arrange
        var metadata = BlobMetadata.Empty.With("width", "1920").Value;

        // Act
        var updated = metadata.Without("WIDTH");

        // Assert
        Assert.Empty(updated.Values);
    }
}
