using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using Xunit;
using MartinDrozdik.DDD.Models.Enumerations;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Statics;

public class FromNameOptionalTests
{
    // Temporary enum for testing
    private class TestEnum : StaticEnumeration<TestEnum>
    {
        public static readonly TestEnum Value1 = new("Value1");
        public static readonly TestEnum Value2 = new("Value2");
    }

    [Fact]
    public void Should_return_null_when_name_is_null()
    {
        // Arrange
        EnumerationName? name = null;

        // Act
        var result = EnumerationMembers.FromNameOptional<TestEnum>(name);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Should_return_enumeration_when_name_is_valid()
    {
        // Arrange
        var name = new EnumerationName(TestEnum.Value1);

        // Act
        var result = EnumerationMembers.FromNameOptional<TestEnum>(name);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TestEnum.Value1, result.Value);
    }

    [Fact]
    public void FromNameOptional_ShouldReturnError_WhenNameIsInvalid()
    {
        // Arrange
        var name = new EnumerationName("InvalidValue");

        // Act
        var result = EnumerationMembers.FromNameOptional<TestEnum>(name);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("NotFound", result.Error.Code.Key);
        Assert.Contains("Name 'InvalidValue' not found.", result.Error.Message);
    }
}
