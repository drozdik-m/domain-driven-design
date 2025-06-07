using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Models.Enumerations;
using MartinDrozdik.DDD.Models.Enumerations.Errors;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Statics;

public class FromNameOptionalTests
{
    private class TestEnum : StaticEnumeration<TestEnum>
    {
        private TestEnum(EnumerationName name) : base(name)
        {
        }

        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
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
    public void Should_return_error_when_name_is_invalid()
    {
        // Arrange
        var name = new EnumerationName("InvalidValue");

        // Act
        var result = EnumerationMembers.FromNameOptional<TestEnum>(name);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(EnumerationErrorCodes.EnumerationNameNotFound, result.Error.Code);
    }
}
