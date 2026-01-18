using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Tests.Extensions;

public class UrlExtensionsTests
{
    [Theory]
    [InlineData("hello-world", "hello-world")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("HELLO WORLD", "hello-world")]
    [InlineData("test123", "test123")]
    [InlineData("café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("Zürich", "zurich")]
    [InlineData("François", "francois")]
    [InlineData("Łódź", "lodz")]
    [InlineData("čeština", "cestina")]
    [InlineData("hello  world", "hello-world")]
    [InlineData("test---slug", "test-slug")]
    [InlineData("  spaces  ", "spaces")]
    [InlineData("-leading-dash", "leading-dash")]
    [InlineData("trailing-dash-", "trailing-dash")]
    [InlineData("hello@world.com", "hello-world-com")]
    [InlineData("test!@#$%slug", "test-slug")]
    [InlineData("price: $19.99", "price-19-99")]
    [InlineData("50% off!", "50-off")]
    [InlineData("/", "")]
    [InlineData("/abc", "abc")]
    [InlineData("file.xml", "file-xml")]
    [InlineData("/abc/some-file.xml", "abc-some-file-xml")]
    [InlineData("ěščřžýáíé", "escrzyaie")]
    [InlineData("ě!!!ščřžýáíé", "e-scrzyaie")]
    [InlineData("___", "")]
    [InlineData("a--b--c", "a-b-c")]
    [InlineData("ÄÖÜß", "aouss")]
    [InlineData("中文文件", "")]
    [InlineData("123___456", "123-456")]
    [InlineData("   ", "")]
    [InlineData("----", "")]
    [InlineData("--a--", "a")]
    public void ToUrlFriendly_returns_expected_slug(string input, string expected)
    {
        Assert.Equal(expected, input.ToUrlFriendly());
    }

    [Theory]
    [InlineData("this is a very long title", 10, "this-is-a")]
    [InlineData("hello-world", 5, "hello")]
    [InlineData("test", 10, "test")]
    [InlineData("word-", 10, "word")]
    [InlineData("hello-world", 11, "hello-world")]
    [InlineData("hello-world", 1, "h")]
    [InlineData("hello world again", 0, "")]
    [InlineData("áéíóú", 3, "aei")]
    [InlineData("--a--b--", 3, "a-b")]
    [InlineData("", 5, "")]
    [InlineData("hello", 0, "")]
    [InlineData("hello", 1, "h")]
    [InlineData("hello", 5, "hello")]
    [InlineData("hello", 6, "hello")]
    public void ToUrlFriendly_with_max_length_truncates_correctly(string input, int maxLength, string expected)
    {
        Assert.Equal(expected, input.ToUrlFriendly(maxLength));
    }

    [Theory]
    [InlineData("my-document.pdf", "my-document.pdf")]
    [InlineData("Photo 2024.jpg", "photo-2024.jpg")]
    [InlineData("file name.txt", "file-name.txt")]
    [InlineData("document", "document")]
    [InlineData("My Document.PDF", "my-document.pdf")]
    [InlineData("Café Photo.JPG", "cafe-photo.jpg")]
    [InlineData("test@file!.txt", "test-file.txt")]
    [InlineData("file.backup.old.txt", "file-backup-old.txt")]
    [InlineData("archive.tar.gz", "archive-tar.gz")]
    [InlineData("ěščřžýáíé.txt", "escrzyaie.txt")]
    [InlineData(".hiddenfile", ".hiddenfile")]
    [InlineData("file.", "file")]
    [InlineData("file..txt", "file.txt")]
    [InlineData("file.###", "file")]
    [InlineData("...txt", ".txt")]
    public void ToUrlFriendlyFileName_valid_filenames_return_expected_result(string input, string expected)
    {
        Assert.Equal(expected, input.ToUrlFriendlyFileName());
    }

    [Theory]
    [InlineData("my-document.pdf", 20, "my-document.pdf")]
    [InlineData("my-document.pdf", 10, "my-doc.pdf")]
    [InlineData("Photo 2024.jpg", 12, "photo-20.jpg")]
    [InlineData("file name.txt", 8, "file.txt")]
    [InlineData("file name.txt", 7, "fil.txt")]
    [InlineData("archive.tar.gz", 12, "archive-t.gz")]
    [InlineData("archive.tar.gz", 10, "archive.gz")]
    [InlineData("ěščřžýáíé.txt", 10, "escrzy.txt")]
    [InlineData("a.txt", 5, "a.txt")]
    [InlineData("Very Long File Name", 10, "very-long")]
    [InlineData(" Very Lon File Name", 10, "very-lon-f")]
    [InlineData("Document", 50, "document")]
    [InlineData("Document", 3, "doc")]
    [InlineData("ěščřžýáíé", 4, "escr")]
    [InlineData("file.###", 10, "file")]
    [InlineData("file..txt", 10, "file.txt")]
    [InlineData(".hiddenfile", 20, ".hiddenfile")]
    [InlineData("...txt", 5, ".txt")]
    [InlineData("longfilename.txt", 8, "long.txt")]
    [InlineData("file.###", 5, "file")]
    public void ToUrlFriendlyFileName_with_max_length_crops_name_preserving_extension(string input, int maxLength, string expected)
    {
        Assert.Equal(expected, input.ToUrlFriendlyFileName(maxLength));
    }

    [Theory]
    [InlineData("file.verylongext", 10)]
    [InlineData("file.1234", 5)]
    [InlineData("a.supercalifragilisticexpialidocious", 20)]
    [InlineData("file.supercalifragilisticexpialidocious", 20)]
    [InlineData("test.longextension", 5)]
    [InlineData("a.extremelylongextension", 5)]
    [InlineData("a.txt", 3)]
    [InlineData("a.txt", 4)]
    public void ToUrlFriendlyFileName_throws_when_extension_too_long(
    string input,
    int maxLength)
    {
        Assert.Throws<ArgumentException>(() =>
            input.ToUrlFriendlyFileName(maxLength));
    }
}
