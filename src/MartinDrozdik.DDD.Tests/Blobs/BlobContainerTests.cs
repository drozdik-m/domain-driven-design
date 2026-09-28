using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class BlobContainerTests
{
    [Theory]
    [InlineData("invoices")]
    [InlineData("user-avatars")]
    [InlineData("a")]
    [InlineData("v1")]
    public void Create_accepts_a_supported_name(string name)
    {
        // Arrange
        // Act
        var container = BlobContainer.Create(name).Value;

        // Assert
        Assert.Equal(name, container.Name);
        Assert.Equal(name, container.ToString());
    }

    [Theory]
    [InlineData("Invoices")]
    [InlineData("invoices/scans")]
    [InlineData("invoices\\scans")]
    [InlineData("invoice_scans")]
    [InlineData("invoice.scans")]
    [InlineData("..")]
    [InlineData("-invoices")]
    [InlineData("invoices-")]
    public void Create_rejects_a_name_that_would_not_travel(string name)
    {
        // Arrange
        // Act
        // Assert
        // The charset is narrow on purpose: the same name has to work as a folder, a URL segment and a bucket
        Assert.True(BlobContainer.Create(name).IsFailure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_name(string name)
    {
        // Arrange
        // Act
        // Assert
        Assert.True(BlobContainer.Create(name).IsFailure);
    }

    [Fact]
    public void Create_returns_the_container_for_a_valid_name()
    {
        // Arrange
        // Act
        var result = BlobContainer.Create("invoices");

        // Assert
        result.IsSuccess();
        Assert.Equal("invoices", result.Value.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Invoices")]
    [InlineData("invoices/scans")]
    [InlineData("-invoices")]
    public void Create_returns_an_invalid_object_error_for_an_invalid_name(string? name)
    {
        // Arrange
        // Act
        var result = BlobContainer.Create(name);

        // Assert
        result.IsFailure();
        Assert.Equal(ErrorCodes.InvalidObject, result.Error.Code);
        Assert.Contains(result.Error.Details, detail => detail.Key == nameof(BlobContainer.Name));
    }

    [Fact]
    public void Create_rejects_a_name_longer_than_the_maximum()
    {
        // Arrange
        var name = new string('a', BlobContainer.MaxLength + 1);

        // Act
        var result = BlobContainer.Create(name);

        // Assert
        result.IsFailure();
        var detail = Assert.Single(result.Error.Details);
        Assert.Contains(BlobContainer.MaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture), detail.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        BlobContainer container1 = "invoices";
        BlobContainer container2 = "invoices";
        BlobContainer differentContainer = "avatars";

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: container1, container1, container2, differentContainer);
        EqualityAssert.TestEqualityOperators<ValueObject>(container1, container2, differentContainer);
    }
}
