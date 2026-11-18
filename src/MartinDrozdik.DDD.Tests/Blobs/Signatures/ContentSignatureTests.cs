using System.Text;
using MartinDrozdik.DDD.Blobs.Signatures;

namespace MartinDrozdik.DDD.Tests.Blobs.Signatures;

public class ContentSignatureTests
{
    [Fact]
    public void A_pattern_is_read_as_hexadecimal_pairs_and_written_back_in_uppercase()
    {
        // Arrange
        // Act
        var signature = new ContentSignature("image/webp", "52 49 46 46  ?? ?? ?? ?? 57 45 42 50 ".ToLowerInvariant());

        // Assert
        Assert.Equal("52 49 46 46 ?? ?? ?? ?? 57 45 42 50", signature.Pattern);
        Assert.Equal(12, signature.Length);
        Assert.Equal("image/webp", signature.MediaType.Value);
    }

    [Fact]
    public void Matches_successfully_returns_true_for_content_that_starts_with_the_pattern()
    {
        // Arrange
        var signature = new ContentSignature("application/pdf", "25 50 44 46 2D");

        // Act
        var matches = signature.Matches(Encoding.ASCII.GetBytes("%PDF-1.7 and more"));

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_successfully_accepts_any_byte_where_the_pattern_has_a_wildcard()
    {
        // Arrange
        var signature = new ContentSignature("image/webp", "52 49 46 46 ?? ?? ?? ?? 57 45 42 50");

        // Act
        var first = signature.Matches(Encoding.ASCII.GetBytes("RIFF\x01\x02\x03\x04WEBPVP8 "));
        var second = signature.Matches(Encoding.ASCII.GetBytes("RIFFzzzzWEBP"));

        // Assert
        Assert.True(first);
        Assert.True(second);
    }

    [Fact]
    public void Matches_successfully_returns_false_for_content_that_starts_differently()
    {
        // Arrange
        var signature = new ContentSignature("application/pdf", "25 50 44 46 2D");

        // Act
        var matches = signature.Matches(Encoding.ASCII.GetBytes("<html>"));

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_successfully_returns_false_for_content_shorter_than_the_pattern()
    {
        // Arrange
        // A truncated header is not a match, even when every byte it has agrees
        var signature = new ContentSignature("application/pdf", "25 50 44 46 2D");

        // Act
        var matches = signature.Matches(Encoding.ASCII.GetBytes("%PDF"));

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData("25 50 4")]
    [InlineData("25 50 GG")]
    [InlineData("2550")]
    [InlineData("25,50")]
    [InlineData("25 ? 50")]
    public void Constructor_throws_for_a_pattern_that_is_not_hexadecimal_pairs(string pattern)
    {
        // Arrange
        // Act
        var create = () => new ContentSignature("application/pdf", pattern);

        // Assert
        Assert.Throws<ArgumentException>("pattern", create);
    }

    [Fact]
    public void Constructor_accepts_a_pattern_of_the_longest_allowed_length()
    {
        // Arrange
        var pattern = string.Join(' ', Enumerable.Repeat("??", ContentSignature.MaxLength));

        // Act
        var signature = new ContentSignature("application/octet-stream", pattern);

        // Assert
        Assert.Equal(ContentSignature.MaxLength, signature.Length);
    }

    [Fact]
    public void Constructor_throws_for_a_pattern_longer_than_the_longest_allowed_length()
    {
        // Arrange
        var pattern = string.Join(' ', Enumerable.Repeat("??", ContentSignature.MaxLength + 1));

        // Act
        var create = () => new ContentSignature("application/octet-stream", pattern);

        // Assert
        Assert.Throws<ArgumentException>("pattern", create);
    }

    [Fact]
    public void Matches_successfully_accepts_any_content_for_a_lone_wildcard()
    {
        // Arrange
        // How a format without a signature, such as plain text, gets on the allow-list
        var signature = new ContentSignature("text/plain", "??");

        // Act
        var text = signature.Matches(Encoding.ASCII.GetBytes("hello"));
        var binary = signature.Matches([0x00]);

        // Assert
        Assert.True(text);
        Assert.True(binary);
    }

    [Fact]
    public void Matches_successfully_returns_false_for_empty_content_with_a_lone_wildcard()
    {
        // Arrange
        // The wildcard still stands for one byte
        var signature = new ContentSignature("text/plain", "??");

        // Act
        var matches = signature.Matches([]);

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData("application/pdf", "%PDF-1.4")]
    [InlineData("image/png", "\x89PNG\r\n\x1A\n")]
    [InlineData("image/gif", "GIF87a")]
    [InlineData("image/gif", "GIF89a")]
    [InlineData("application/zip", "PK\x03\x04")]
    [InlineData("video/mp4", "\0\0\0\u0018ftypmp42")]
    [InlineData("image/jpeg", "\xFF\xD8\xFF\xE2")]
    [InlineData("audio/mpeg", "ID3\x04")]
    [InlineData("application/vnd.rar", "Rar!\x1A\x07\x01\x00")]
    [InlineData("application/msword", "\xD0\xCF\x11\xE0\xA1\xB1\x1A\xE1")]
    [InlineData("font/woff2", "wOF2")]
    [InlineData("video/3gpp", "\0\0\0\u0014ftyp3gp5")]
    public void The_common_signatures_recognise_the_start_of_their_formats(string mediaType, string header)
    {
        // Arrange
        var bytes = Encoding.Latin1.GetBytes(header);

        // Act
        var matches = ContentSignature.Common.Where(s => s.MediaType.Value == mediaType).Any(s => s.Matches(bytes));

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void The_common_signatures_recognise_a_tar_archive_by_its_magic_at_offset_257()
    {
        // Arrange
        var header = new byte[512];
        Encoding.ASCII.GetBytes("ustar\u000000").CopyTo(header, 257);

        // Act
        var matches = ContentSignature.Common.Where(s => s.MediaType.Value == "application/x-tar").Any(s => s.Matches(header));

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void The_common_signatures_leave_out_executables()
    {
        // Arrange
        // Common is a safe default; executables have to be listed on purpose
        var executables = new[] { "application/x-msdownload", "application/vnd.microsoft.portable-executable", "application/x-executable", "application/java-archive" };

        // Act
        var listed = ContentSignature.Common.Select(s => s.MediaType.Value).Intersect(executables);

        // Assert
        Assert.Empty(listed);
    }
}
