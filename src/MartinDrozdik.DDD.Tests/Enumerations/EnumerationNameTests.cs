using MartinDrozdik.DDD.Models.Enumerations;

namespace MartinDrozdik.DDD.Tests.Enumerations;

public class EnumerationNameTests
{
    [Theory]
    [InlineData("TestName")]
    [InlineData("AnotherTestName")]
    [InlineData("Test name with spaces")]
    [InlineData("123")]
    [InlineData("Test_Name")]
    [InlineData("Test-Name")]
    public void Can_construct_valid_name(string key)
    {
        var exception = Record.Exception(() => new EnumerationName(key));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Constructor_should_throw_exception_for_empty_keys(string? emptyKey)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EnumerationName(emptyKey!));
    }

    [Fact]
    public void Constructor_should_throw_null_exception_for_null_input()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EnumerationName(null!));
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        EnumerationName name1 = "TestName";
        EnumerationName name2 = "TestName";
        EnumerationName differentName = "DifferentName";

        // Act & Assert
        EqualityAssertions.TestAllEqualityBehaviors(
            name1,
            name2,
            differentName);
    }
}
