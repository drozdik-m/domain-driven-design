using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Tests.Extensions;

public class PathExtensionsTests
{
    [Theory]
    [InlineData("hello world", "hello-world")]
    [InlineData("test  file", "test--file")]
    [InlineData("my file.txt", "my-file.txt")]
    [InlineData("file", "file")]
    [InlineData("", "")]
    [InlineData("test\tfile", "test-file")]
    [InlineData("test\nfile", "test-file")]
    [InlineData("test\r\nfile", "test--file")]
    [InlineData("file<name>", "file-name-")]
    [InlineData("file:name", "file-name")]
    [InlineData("file|name", "file-name")]
    [InlineData("file?name", "file-name")]
    [InlineData("file*name", "file-name")]
    [InlineData("file\"name", "file-name")]
    [InlineData("file/name", "file-name")]
    [InlineData("file\\name", "file-name")]
    [InlineData("|filename", "-filename")]
    [InlineData("filename|", "filename-")]
    [InlineData("valid-filename.txt", "valid-filename.txt")]
    [InlineData("my_file_123.doc", "my_file_123.doc")]
    [InlineData("test.file.name", "test.file.name")]
    [InlineData("file name with multiple   spaces", "file-name-with-multiple---spaces")]
    [InlineData("   leading spaces", "---leading-spaces")]
    [InlineData("trailing spaces   ", "trailing-spaces---")]
    public void ToFriendlyFileName_returns_expected_result(string input, string expected)
    {
        Assert.Equal(expected, input.ToFriendlyFileName());
    }

    [Theory]

    // Only the last segment survives (prevent directory traversal)
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("..\\..\\windows\\system.ini", "system.ini")]
    [InlineData("/etc/passwd", "passwd")]
    [InlineData("/passwd", "passwd")]
    [InlineData("C:\\Windows\\notepad.exe", "notepad.exe")]
    [InlineData("../../../../../../../../etc/shadow", "shadow")]
    [InlineData("....//....//etc/passwd", "passwd")]
    [InlineData("..%2f..%2fetc/passwd", "passwd")]
    [InlineData("foo/bar\\baz.txt", "baz.txt")]
    [InlineData("a/../b.txt", "b.txt")]
    [InlineData("\\\\server\\share\\secret.txt", "secret.txt")]
    [InlineData("\\\\?\\C:\\Windows\\win.ini", "win.ini")]
    [InlineData(".ssh/authorized_keys", "authorized_keys")]

    // A path that ends in a separator carries no name at all
    [InlineData("/etc/passwd/", "file")]
    [InlineData("/", "file")]
    [InlineData("///", "file")]
    [InlineData("\\", "file")]
    [InlineData("uploads/..", "file")]
    [InlineData("uploads/.", "file")]

    // Traversal spelled in an encoding this method does not decode stays one inert segment
    [InlineData("..%2f..%2fetc%2fpasswd", "%2f..%2fetc%2fpasswd")]
    [InlineData("%2e%2e%2f%2e%2e%2fetc%2fpasswd", "%2e%2e%2f%2e%2e%2fetc%2fpasswd")]

    // Relative segments and hidden files cannot be produced (prevent directory traversal)
    [InlineData("..", "file")]
    [InlineData(".", "file")]
    [InlineData("...", "file")]
    [InlineData(".htaccess", "htaccess")]
    [InlineData(" .. ", "file")]
    [InlineData("..hidden..", "hidden")]
    [InlineData(".env", "env")]

    // A null byte must not be able to cut the name short for a native API
    [InlineData("safe.txt\0.exe", "safe.txt-.exe")]

    // Control characters smuggled in as whitespace
    [InlineData("in\nvoice.pdf", "in-voice.pdf")]
    [InlineData("in\tvoice.pdf", "in-voice.pdf")]

    // NTFS alternate data streams and drive-relative names both hinge on the colon
    [InlineData("report.pdf:Zone.Identifier", "report.pdf-Zone.Identifier")]
    [InlineData("C:report.pdf", "C-report.pdf")]

    // Windows silently strips these, which would desynchronize the stored name from the recorded one
    [InlineData("report.pdf.", "report.pdf")]
    [InlineData("report.pdf ", "report.pdf")]
    [InlineData("  report.pdf  ", "report.pdf")]

    // Device names are not creatable on Windows, with or without an extension
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("NUL.log", "_NUL.log")]
    [InlineData("COM1", "_COM1")]
    [InlineData("LPT9.dat", "_LPT9.dat")]
    [InlineData("console.txt", "console.txt")]
    [InlineData("AUX", "_AUX")]
    [InlineData("PRN.tar.gz", "_PRN.tar.gz")]
    [InlineData("COM0", "_COM0")]
    [InlineData("LPT0", "_LPT0")]
    [InlineData("CoM1.TxT", "_CoM1.TxT")]
    [InlineData("CON. ", "_CON")]
    [InlineData("  nul  ", "_nul")]
    [InlineData("C:\\Windows\\CON", "_CON")]
    [InlineData("COM10", "COM10")]
    [InlineData("_CON", "_CON")]

    // Nothing usable left
    [InlineData("", "file")]
    [InlineData("   ", "file")]
    [InlineData("\t\n", "file")]
    [InlineData("///...///", "file")]
    [InlineData("<>|?*", "-----")]

    // Ordinary names are left readable
    [InlineData("Faktura 2026 (final).pdf", "Faktura-2026-(final).pdf")]
    [InlineData("my_file_123.doc", "my_file_123.doc")]
    [InlineData("report.PDF", "report.PDF")]
    public void ToStoredFileName_returns_expected_result(string input, string expected)
    {
        // Arrange
        // Act
        var result = input.ToStoredFileName();

        // Assert
        Assert.Equal(expected, result);

        // Idempotent
        Assert.Equal(result, result.ToStoredFileName());
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("..\\..\\..\\Windows\\System32\\config\\SAM")]
    [InlineData("....//....//....//etc/shadow")]
    [InlineData("/absolute/secret.txt")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("\\\\server\\share\\secret.txt")]
    [InlineData("\\\\?\\C:\\Windows\\win.ini")]
    [InlineData("..")]
    [InlineData("../")]
    [InlineData("..\\")]
    [InlineData(".")]
    [InlineData("uploads/../../etc/passwd")]
    [InlineData("..%2f..%2fetc%2fpasswd")]
    [InlineData("%2e%2e/%2e%2e/etc/passwd")]
    [InlineData("safe.txt\0../../etc/passwd")]
    [InlineData("report.pdf:Zone.Identifier")]
    [InlineData("C:report.pdf")]
    [InlineData("CON")]
    [InlineData("\r\n../secret")]
    [InlineData("")]
    [InlineData("   ")]
    public void ToStoredFileName_never_escapes_the_folder_it_is_combined_with(string input)
    {
        // Arrange
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "blobs"));

        // Act
        var result = input.ToStoredFileName();

        // Assert
        Assert.NotEmpty(result);
        Assert.DoesNotContain("/", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\\", result, StringComparison.Ordinal);
        Assert.DoesNotContain(":", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\0", result, StringComparison.Ordinal);
        Assert.NotEqual(".", result);
        Assert.NotEqual("..", result);

        // The name is a single segment that the file system resolves inside the root
        Assert.Equal(result, Path.GetFileName(result));

        var combined = Path.GetFullPath(Path.Combine(root, result));
        Assert.StartsWith(root + Path.DirectorySeparatorChar, combined, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStoredFileName_shortens_an_overlong_name_but_keeps_its_extension()
    {
        // Arrange
        var name = new string('a', PathExtensions.MaxStoredFileNameLength * 2) + ".pdf";

        // Act
        var result = name.ToStoredFileName();

        // Assert
        Assert.Equal(PathExtensions.MaxStoredFileNameLength, result.Length);
        Assert.EndsWith(".pdf", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStoredFileName_shortens_an_overlong_name_that_has_no_extension()
    {
        // Arrange
        var name = new string('a', PathExtensions.MaxStoredFileNameLength * 2);

        // Act
        var result = name.ToStoredFileName();

        // Assert
        Assert.Equal(PathExtensions.MaxStoredFileNameLength, result.Length);
    }

    [Fact]
    public void ToStoredFileName_shortens_an_overlong_name_that_arrives_as_a_path()
    {
        // Arrange
        var name = "../../etc/" + new string('a', PathExtensions.MaxStoredFileNameLength * 2) + ".pdf";

        // Act
        var result = name.ToStoredFileName();

        // Assert
        Assert.Equal(PathExtensions.MaxStoredFileNameLength, result.Length);
        Assert.EndsWith(".pdf", result, StringComparison.Ordinal);
        Assert.Equal(result, Path.GetFileName(result));
    }

    [Fact]
    public void ToStoredFileName_drops_an_implausibly_long_extension_when_shortening()
    {
        // Arrange
        var name = new string('a', PathExtensions.MaxStoredFileNameLength - 1)
            + "."
            + new string('b', 100);

        // Act
        var result = name.ToStoredFileName();

        // Assert
        Assert.Equal(new string('a', PathExtensions.MaxStoredFileNameLength - 1), result);
        Assert.DoesNotContain(".", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStoredFileName_escapes_a_device_name_that_shortening_uncovers()
    {
        // Arrange
        var name = "CON." + new string('a', PathExtensions.MaxStoredFileNameLength * 2);

        // Act
        var result = name.ToStoredFileName();

        // Assert
        Assert.Equal(PathExtensions.MaxStoredFileNameLength, result.Length);
        Assert.StartsWith("_CON.", result, StringComparison.Ordinal);
    }
}
