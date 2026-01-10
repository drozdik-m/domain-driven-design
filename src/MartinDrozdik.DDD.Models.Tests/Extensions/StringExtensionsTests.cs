using MartinDrozdik.DDD.Models.Extensions;

namespace MartinDrozdik.DDD.Models.Tests.Extensions;

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
    [InlineData("x", "X")]
    [InlineData("X", "X")]
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
}
