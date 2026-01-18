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
}
