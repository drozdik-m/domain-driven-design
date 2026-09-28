using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs;

public class BlobNameTests
{
    [Theory]
    [InlineData("report.pdf", "pdf")]
    [InlineData("report.PDF", "pdf")]
    [InlineData("archive.tar.gz", "gz")]
    [InlineData("noextension", null)]
    public void Extension_is_lowercase_and_without_the_dot(string value, string? expected)
    {
        // Arrange
        // Act
        var name = BlobName.FromFileName(value);

        // Assert
        Assert.Equal(expected, name.Extension);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("scans/")]
    public void FromFileName_falls_back_when_nothing_usable_is_left(string? value)
    {
        // Arrange
        // Act
        var name = BlobName.FromFileName(value);

        // Assert
        Assert.Equal(BlobName.Fallback, name.Value);
    }

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\scans\\report.pdf", "report.pdf")]
    [InlineData("  Faktura 2026.pdf  ", "Faktura-2026.pdf")]
    [InlineData("a:b*c?.txt", "a-b-c-.txt")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData(".hidden", "hidden")]
    public void FromFileName_tidies_the_name(string original, string expected)
    {
        // Arrange
        // Act
        var name = BlobName.FromFileName(original);

        // Assert
        Assert.Equal(expected, name.Value);
    }

    [Fact]
    public void FromFileName_caps_the_length_and_keeps_the_extension()
    {
        // Arrange
        var original = new string('a', 400) + ".pdf";

        // Act
        var name = BlobName.FromFileName(original);

        // Assert
        Assert.Equal(BlobName.MaxLength, name.Value.Length);
        Assert.Equal("pdf", name.Extension);
    }

    [Fact]
    public void FromStored_keeps_the_name_as_it_is()
    {
        // Arrange
        // Stored under older rules - reading it back must not rename it
        const string stored = "Faktura 2026.pdf";

        // Act
        var name = BlobName.FromStored(stored);

        // Assert
        Assert.Equal(stored, name.Value);
        Assert.Equal("pdf", name.Extension);
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var name1 = BlobName.FromFileName("report.pdf");
        var name2 = BlobName.FromFileName("report.pdf");
        var differentName = BlobName.FromFileName("other.pdf");

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: name1, name1, name2, differentName);
        EqualityAssert.TestEqualityOperators<ValueObject>(name1, name2, differentName);
    }
}
