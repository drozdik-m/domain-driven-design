using MartinDrozdik.DDD.Models.Extensions;

namespace MartinDrozdik.DDD.Models.Tests.Extensions;

public class CurrencyFormatExtensionsTests
{
    [Theory]
    [InlineData(int.MinValue, "-2 147 483 648,00 Kč")]
    [InlineData(-123456789, "-123 456 789,00 Kč")]
    [InlineData(-12345678, "-12 345 678,00 Kč")]
    [InlineData(-1234567, "-1 234 567,00 Kč")]
    [InlineData(-123456, "-123 456,00 Kč")]
    [InlineData(-12345, "-12 345,00 Kč")]
    [InlineData(-1234, "-1 234,00 Kč")]
    [InlineData(-123, "-123,00 Kč")]
    [InlineData(-12, "-12,00 Kč")]
    [InlineData(-1, "1,00 Kč")]
    [InlineData(0, "0,00 Kč")]
    [InlineData(1, "1,00 Kč")]
    [InlineData(12, "12,00 Kč")]
    [InlineData(123, "123,00 Kč")]
    [InlineData(1234, "1 234,00 Kč")]
    [InlineData(12345, "12 345,00 Kč")]
    [InlineData(123456, "123 456,00 Kč")]
    [InlineData(1234567, "1 234 567,00 Kč")]
    [InlineData(12345678, "12 345 678,00 Kč")]
    [InlineData(123456789, "123 456 789,00 Kč")]
    [InlineData(int.MaxValue, "2 147 483 647,00 Kč")]
    public void To_czk_converts_correctly(int toConvert, string expected)
    {
        Assert.Equal(expected, toConvert.ToCzk());
    }
}
