using MartinDrozdik.DDD.Models.Extensions;

namespace MartinDrozdik.DDD.Models.Tests.Extensions;

public class RandomExtensionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(256)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(4000)]
    [InlineData(8000)]
    public void Random_string_returns_correct_length_with(int length)
    {
        var random = new Random(67);
        var result = random.String(length, "abc");

        Assert.Equal(length, result.Length);
    }

    [Theory]
    [InlineData(RandomExtensions.Sets.Letters)]
    [InlineData(RandomExtensions.Sets.Digits)]
    [InlineData(RandomExtensions.Sets.Alphanumeric)]
    [InlineData(RandomExtensions.Sets.Hex)]
    [InlineData(RandomExtensions.Sets.Lowercase)]
    [InlineData(RandomExtensions.Sets.Uppercase)]
    [InlineData(RandomExtensions.Sets.UrlSafe)]
    [InlineData(RandomExtensions.Sets.SpecialChars)]
    [InlineData(RandomExtensions.Sets.AlphanumericSpecial)]
    public void Random_string_returns_only_allowed_characters(string characters)
    {
        var random = new Random(69);
        var resultShort = random.String(64, characters);
        var resultLong = random.String(2000, characters);

        Assert.Multiple(
            () => Assert.All(resultShort, e => Assert.Contains(e, characters)),
            () => Assert.All(resultLong, e => Assert.Contains(e, characters)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Random_string_throws_on_negative_length(int length)
    {
        var random = new Random(71);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.String(length, "abc"));
    }

    [Fact]
    public void Empty_allowed_characters_throws()
    {
        var random = new Random(73);
        Assert.Throws<ArgumentException>(() => random.String(10, string.Empty));
    }
}
