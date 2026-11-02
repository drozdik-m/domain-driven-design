using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Tests.Extensions;

public class StringExtensionsTests
{
    /// <summary>
    /// 512 dashes for testing longer data.
    /// </summary>
    private const string Dashes512 = "--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------";

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("hello", "Hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("HELLO", "HELLO")]
    [InlineData("hello world", "Hello world")]
    [InlineData("Hello World", "Hello World")]
    [InlineData("123abc", "123abc")]
    [InlineData("ábcd", "Ábcd")]
    [InlineData("Ábcd", "Ábcd")]
    [InlineData("čeština", "Čeština")]
    [InlineData("Čeština", "Čeština")]
    [InlineData("ř", "Ř")]
    [InlineData("Ř", "Ř")]
    [InlineData("test with multiple words", "Test with multiple words")]
    [InlineData("UPPERCASE SENTENCE", "UPPERCASE SENTENCE")]
    [InlineData("lowercase sentence", "Lowercase sentence")]
    [InlineData("1234567890", "1234567890")]
    [InlineData("!exclamation", "!exclamation")]
    [InlineData(" leading space", " leading space")]
    public void FirstToUpper_returns_expected_result(string input, string expected)
    {
        Assert.Equal(expected, input.FirstToUpper());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("hello", "Hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("HELLO", "HELLO")]
    [InlineData("hello world", "Hello world")]
    [InlineData("čeština", "Čeština")]
    [InlineData("Čeština", "Čeština")]
    [InlineData("test with multiple words", "Test with multiple words")]
    [InlineData("1234567890", "1234567890")]
    [InlineData("!exclamation", "!exclamation")]
    [InlineData(" leading space", " leading space")]
    public void FirstToUpperOptional_returns_expected_result(string? input, string? expected)
    {
        Assert.Equal(expected, input.FirstToUpperOptional());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    [InlineData("Hello", "hello")]
    [InlineData("hello", "hello")]
    [InlineData("HELLO", "hELLO")]
    [InlineData("Hello world", "hello world")]
    [InlineData("hello world", "hello world")]
    [InlineData("123abc", "123abc")]
    [InlineData("Ábcd", "ábcd")]
    [InlineData("ábcd", "ábcd")]
    [InlineData("Čeština", "čeština")]
    [InlineData("čeština", "čeština")]
    [InlineData("Ř", "ř")]
    [InlineData("ř", "ř")]
    [InlineData("Test with multiple words", "test with multiple words")]
    [InlineData("lowercase sentence", "lowercase sentence")]
    [InlineData("Lowercase sentence", "lowercase sentence")]
    [InlineData("1234567890", "1234567890")]
    [InlineData("!exclamation", "!exclamation")]
    [InlineData(" leading space", " leading space")]
    public void FirstToLower_returns_expected_result(string input, string expected)
    {
        Assert.Equal(expected, input.FirstToLower());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    [InlineData("Hello", "hello")]
    [InlineData("hello", "hello")]
    [InlineData("HELLO", "hELLO")]
    [InlineData("Hello world", "hello world")]
    [InlineData("Čeština", "čeština")]
    [InlineData("čeština", "čeština")]
    [InlineData("Test with multiple words", "test with multiple words")]
    [InlineData("1234567890", "1234567890")]
    [InlineData("!exclamation", "!exclamation")]
    [InlineData(" leading space", " leading space")]
    public void FirstToLowerOptional_returns_expected_result(string? input, string? expected)
    {
        Assert.Equal(expected, input.FirstToLowerOptional());
    }

    [Theory]
    [InlineData(Dashes512, "-", "-")]
    [InlineData(Dashes512 + "-", "-", "-")]
    [InlineData("aaabcaa", "a", "abca")]
    [InlineData("aaa", "a", "a")]
    [InlineData("abc", "a", "abc")]
    [InlineData("a", "a", "a")]
    [InlineData("aa", "a", "a")]
    [InlineData("aba", "a", "aba")]
    [InlineData("aabaa", "a", "aba")]
    [InlineData("bbbbb", "b", "b")]
    [InlineData("abababab", "a", "abababab")]
    [InlineData("xyz", "a", "xyz")]
    [InlineData("!!!!", "!", "!")]
    [InlineData("   ", " ", " ")]
    [InlineData("...", ".", ".")]
    [InlineData("___", "_", "_")]
    public void RemoveNeighbourlyRepeatingString_single_char_returns_expected_result(
    string input, string pattern, string expected)
    {
        Assert.Multiple(
            () => Assert.Equal(expected, input.RemoveNeighbourlyRepeatingString(pattern)), // Short input
            () => Assert.Equal(expected + Dashes512, input.RemoveNeighbourlyRepeatingString(pattern) + Dashes512), // Long input
            () => Assert.Equal(Dashes512 + expected, Dashes512 + input.RemoveNeighbourlyRepeatingString(pattern))); // Long input
    }

    [Theory]
    [InlineData("abcabcabc", "abc", "abc")]
    [InlineData("abcabc", "abc", "abc")]
    [InlineData("abc", "abc", "abc")]
    [InlineData("abcdefabc", "abc", "abcdefabc")]
    [InlineData("xyzxyzxyz", "xyz", "xyz")]
    [InlineData("ababab", "ab", "ab")]
    [InlineData("abcabcdefabcabc", "abc", "abcdefabc")]
    [InlineData("testtest", "test", "test")]
    [InlineData("testtesttest", "test", "test")]
    [InlineData("aabbaabb", "aa", "aabbaabb")]
    [InlineData("aaaaaa", "aa", "aa")]
    [InlineData("aaaaaaa", "aa", "aaa")]
    [InlineData("aaaa", "aa", "aa")]
    [InlineData("hello world", "xyz", "hello world")]
    [InlineData("test", "abc", "test")]
    [InlineData("ab", "abc", "ab")]
    [InlineData("a", "abc", "a")]
    [InlineData("test", "testing", "test")]
    [InlineData("abcdef", "abc", "abcdef")]
    [InlineData("test", "test", "test")]
    [InlineData("xyzabc", "abc", "xyzabc")]
    [InlineData("ababcabc", "abc", "ababc")]
    [InlineData("abcabdabc", "abc", "abcabdabc")]
    [InlineData("aabaab", "aab", "aab")]
    [InlineData("aabaabaac", "aab", "aabaac")]
    [InlineData("aaaaaaaa", "aa", "aa")]
    [InlineData("abababab", "abab", "abab")]
    [InlineData("aaaxaaa", "aaa", "aaaxaaa")]
    [InlineData("xaaayyy", "aaa", "xaaayyy")]
    [InlineData("aaaaaaaxaaa", "aaa", "aaaaxaaa")]
    [InlineData("xaaaaaaa", "aaa", "xaaaa")]
    [InlineData("čččaččč", "čč", "čččaččč")]
    [InlineData("ářářař", "ář", "ářař")]
    [InlineData("123123123", "123", "123")]
    public void RemoveNeighbourlyRepeatingString_multi_char_returns_expected_result(
        string input, string pattern, string expected)
    {
        Assert.Multiple(
            () => Assert.Equal(expected, input.RemoveNeighbourlyRepeatingString(pattern)), // Short input
            () => Assert.Equal(expected + Dashes512, input.RemoveNeighbourlyRepeatingString(pattern) + Dashes512), // Long input
            () => Assert.Equal(Dashes512 + expected, Dashes512 + input.RemoveNeighbourlyRepeatingString(pattern))); // Long input
    }

    [Theory]
    [InlineData("", "a", "")]
    [InlineData("a", "", "a")]
    [InlineData("abc", "", "abc")]
    [InlineData("", "", "")]
    public void RemoveNeighbourlyRepeatingString_empty_returns_expected_result(
        string input, string pattern, string expected)
    {
        Assert.Equal(expected, input.RemoveNeighbourlyRepeatingString(pattern));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("abc", 10)]
    public void Truncate_returns_the_text_itself_when_it_fits(string text, int maxLength)
    {
        // Arrange
        // Act
        var result = text.Truncate(maxLength);

        // Assert
        Assert.Same(text, result);
    }

    [Theory]
    [InlineData("Hello", 0, "")]
    [InlineData("Hello", 1, "H")]
    [InlineData("Hello", 2, "He")]
    [InlineData("Hello", 3, "Hel")]
    [InlineData("Hello", 4, "Hell")]
    [InlineData("Hello", 5, "Hello")]
    [InlineData("Hello", 6, "Hello")]
    [InlineData("Hello", 7, "Hello")]
    [InlineData("čeština", 3, "češ")]
    [InlineData("ab\U0001F600cd", 3, "ab")] // Surrogate pair
    [InlineData("ab\U0001F600cd", 4, "ab\U0001F600")] // Surrogate pair that ends exactly at the limit
    [InlineData("cafe\u0301 au lait", 4, "caf")] // Combining accent
    [InlineData("a\U0001F44D\U0001F3FD", 4, "a")] // Skin tone modifier
    [InlineData("a\U0001F468\u200D\U0001F469\u200D\U0001F467", 6, "a")] // Emoji joined by zero width joiners
    [InlineData("a\U0001F1E8\U0001F1FF", 3, "a")] // Flag made of two regional indicators
    [InlineData("a\r\nb", 2, "a")] // CRLF
    [InlineData("e\u0301\u0301\u0301\u0301\u0301\u0301", 4, "e\u0301\u0301\u0301")] // First char is longer then maxLength
    [InlineData("\U0001F44D\U0001F3FD", 3, "\U0001F44D")] // First char is longer then maxLength
    public void Truncate_cuts_plain_text_at_the_maximum_length(string text, int maxLength, string expected)
    {
        // Arrange
        // Act
        var result = text.Truncate(maxLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Truncate_returns_a_valid_prefix_for_every_maximum_length()
    {
        // Arrange
        const string text = "Ahoj \U0001F600 cafe\u0301 \U0001F44D\U0001F3FD \U0001F468\u200D\U0001F469\u200D\U0001F467 \U0001F1E8\U0001F1FF\r\nkonec";

        for (var maxLength = 0; maxLength <= text.Length + 5; maxLength++)
        {
            // Act
            var result = text.Truncate(maxLength);

            // Assert
            Assert.True(result.Length <= maxLength, $"The result for {maxLength} is {result.Length} long.");
#pragma warning disable S3415 // Assertion arguments should be passed in the correct order
            Assert.StartsWith(result, text, StringComparison.Ordinal);
#pragma warning restore S3415
            Assert.False(result.Length > 0 && char.IsHighSurrogate(result[^1]), $"The result for {maxLength} ends with half of a surrogate pair.");
        }
    }

    [Fact]
    public void Truncate_throws_for_a_negative_maximum_length()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".Truncate(-1));
    }

    [Fact]
    public void Truncate_throws_for_null()
    {
        // Arrange
        string text = null!;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => text.Truncate(1));
    }
}
