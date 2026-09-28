using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Results.Exceptions;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class MediaTypeTests
{
    [Theory]
    [InlineData("image/png", "image", "png")]
    [InlineData("application/pdf", "application", "pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application", "vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("image/svg+xml", "image", "svg+xml")]
    public void Create_splits_a_supported_type(string value, string expectedType, string expectedSubType)
    {
        // Arrange
        // Act
        var result = MediaType.Create(value);

        // Assert
        result.IsSuccess();
        Assert.Equal(value, result.Value.Value);
        Assert.Equal(expectedType, result.Value.Type);
        Assert.Equal(expectedSubType, result.Value.SubType);
    }

    [Theory]
    [InlineData("TEXT/PLAIN", "text/plain")]
    [InlineData("text/plain; charset=utf-8", "text/plain")]
    [InlineData("  text/plain  ", "text/plain")]
    public void Create_keeps_only_the_essence(string value, string expected)
    {
        // Arrange
        // Act
        var result = MediaType.Create(value);

        // Assert
        // A charset describes one response, not the stored file
        result.IsSuccess();
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("image")]
    [InlineData("/png")]
    [InlineData("image/")]
    [InlineData("image png")]
    [InlineData("image/png/extra")]
    public void Create_returns_an_invalid_object_error_for_something_that_is_not_a_type_and_a_subtype(string? value)
    {
        // Arrange
        // Act
        var result = MediaType.Create(value);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
        Assert.Contains(result.Error.Details, detail => detail.Key == nameof(MediaType.Value));
    }

    [Fact]
    public void Create_rejects_a_type_longer_than_the_maximum()
    {
        // Arrange
        var value = "application/" + new string('a', MediaType.MaxLength);

        // Act
        var result = MediaType.Create(value);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
    }

    [Fact]
    public void Implicit_conversion_throws_for_an_invalid_type()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ResultFailureException<Error>>(() => (MediaType)"image");
    }

    [Fact]
    public void OctetStream_is_the_safe_default()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal("application/octet-stream", MediaType.OctetStream.Value);
        Assert.Equal(MediaType.Create("application/octet-stream").Value, MediaType.OctetStream);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        MediaType mediaType1 = "image/png";
        MediaType mediaType2 = "IMAGE/PNG";
        MediaType differentMediaType = "image/jpeg";

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: mediaType1, mediaType1, mediaType2, differentMediaType);
        EqualityAssert.TestEqualityOperators<ValueObject>(mediaType1, mediaType2, differentMediaType);
    }
}
