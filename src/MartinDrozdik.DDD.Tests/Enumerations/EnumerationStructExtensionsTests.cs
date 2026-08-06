using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Enumerations.Attributes;
using MartinDrozdik.DDD.Exceptions;

namespace MartinDrozdik.DDD.Tests.Enumerations;

public class EnumerationStructExtensionsTests
{
    private enum TestState
    {
        One,
        Two,

        [EnumerationName("Three")]
        Third,
    }

    [Fact]
    public void ToStructEnum_returns_the_struct_enum_member_of_the_same_name()
    {
        // Arrange
        var enumeration = TestEnum.One;

        // Act
        var result = enumeration.ToStructEnum<TestState>();

        // Assert
        Assert.Equal(TestState.One, result);
    }

    [Fact]
    public void ToStructEnum_honors_the_enumeration_name_attribute()
    {
        // Arrange
        var enumeration = TestEnum.Three;

        // Act
        var result = enumeration.ToStructEnum<TestState>();

        // Assert
        Assert.Equal(TestState.Third, result);
    }

    [Fact]
    public void ToStructEnum_throws_for_an_enumeration_member_without_a_counterpart()
    {
        // Arrange
        var enumeration = TestEnum.Unmapped;

        // Act
        var exception = Assert.Throws<BusinessRuleException>(() => enumeration.ToStructEnum<TestState>());

        // Assert
        Assert.Contains(nameof(TestState), exception.Message, StringComparison.Ordinal);
        Assert.Equal(nameof(TestState), Assert.Single(exception.DetailsDictionary["StructEnum"]));
        Assert.Equal(nameof(TestEnum.Unmapped), Assert.Single(exception.DetailsDictionary["Name"]));
    }

    [Fact]
    public void ToStructEnum_throws_for_a_null_enumeration()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => EnumerationStructExtensions.ToStructEnum<TestState>(null!));
    }

    [Fact]
    public void ToStructEnumOptional_returns_the_struct_enum_member_of_the_same_name()
    {
        // Arrange
        Enumeration? enumeration = TestEnum.Two;

        // Act
        var result = enumeration.ToStructEnumOptional<TestState>();

        // Assert
        Assert.Equal(TestState.Two, result);
    }

    [Fact]
    public void ToStructEnumOptional_returns_null_for_a_null_enumeration()
    {
        // Arrange
        Enumeration? enumeration = null;

        // Act
        var result = enumeration.ToStructEnumOptional<TestState>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToEnumerationName_returns_the_struct_enum_member_name()
    {
        // Act
        var result = TestState.One.ToEnumerationName();

        // Assert
        Assert.Equal(new EnumerationName(nameof(TestState.One)), result);
    }

    [Fact]
    public void ToEnumerationName_honors_the_enumeration_name_attribute()
    {
        // Act
        var result = TestState.Third.ToEnumerationName();

        // Assert
        Assert.Equal(new EnumerationName("Three"), result);
    }

    [Fact]
    public void ToEnumerationName_returns_the_raw_value_for_an_undefined_member()
    {
        // Arrange
        var value = (TestState)99;

        // Act
        var result = value.ToEnumerationName();

        // Assert
        Assert.Equal(new EnumerationName("99"), result);
    }

    [Fact]
    public void Struct_enum_members_survive_a_round_trip()
    {
        // Arrange
        TestState[] mappedValues = [TestState.One, TestState.Two, TestState.Third];

        // Act
        var result = mappedValues
            .Select(value => TestEnum.FromStructEnum(value).ToStructEnum<TestState>())
            .ToArray();

        // Assert
        Assert.Equal(mappedValues, result);
    }

    private class TestEnum(EnumerationName name) : StaticEnumeration<TestEnum>(name)
    {
        public static readonly TestEnum One = new(nameof(One));
        public static readonly TestEnum Two = new(nameof(Two));
        public static readonly TestEnum Three = new(nameof(Three));
        public static readonly TestEnum Unmapped = new(nameof(Unmapped));
    }
}
