using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Enumerations.Attributes;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Tests.Enumerations.Assertions;

namespace MartinDrozdik.DDD.Tests.Enumerations;

public class StaticEnumerationTests
{
    private enum TestState
    {
        Value1,

        [EnumerationName(nameof(TestEnum.Value2))]
        Second,

        Missing,
    }

    [Fact]
    public void FromStructEnum_honors_the_enumeration_name_attribute()
    {
        // Arrange
        var value = TestState.Second;

        // Act
        var result = TestEnum.FromStructEnum(value);

        // Assert
        Assert.Equal(TestEnum.Value2, result);
    }

    [Fact]
    public void FromStructEnum_throws_for_an_undefined_struct_enum_value()
    {
        // Arrange
        var value = (TestState)99;

        // Act
        var exception = Assert.Throws<BusinessRuleException>(() => TestEnum.FromStructEnum(value));

        // Assert
        Assert.Equal("99", Assert.Single(exception.DetailsDictionary["Name"]));
    }

    [Fact]
    public void Should_implement_IStructEnumDeserializer_correctly()
    {
        // Arrange
        var validValue = TestState.Value1;
        var invalidValue = TestState.Missing;
        var expectedValue = TestEnum.Value1;

        // Act & Assert
        StructEnumDeserializerAssertions.AssertStructEnumDeserializer(
            validValue,
            invalidValue,
            expectedValue);
    }

    [Fact]
    public void Should_implement_IEnumerationDeserializer_correctly()
    {
        // Arrange
        var validName = TestEnum.Value1.Name;
        var invalidName = new EnumerationName("InvalidValue");
        var expectedValue = TestEnum.Value1;

        // Act & Assert
        EnumerationDeserializerAssertions.AssertEnumerationDeserializer(
            validName,
            invalidName,
            expectedValue);
    }

    [Fact]
    public void Should_implement_IEnumerationEnumerator_correctly()
    {
        // Arrange
        var all = EnumerationMembers.GetAllStaticMembers<TestEnum>();

        // Act & Assert
        EnumerationEnumeratorAssertions.AssertGetAll(all);
    }

    private class TestEnum(EnumerationName name) : StaticEnumeration<TestEnum>(name)
    {
        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
    }
}
