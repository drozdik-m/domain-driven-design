using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using Xunit;
using MartinDrozdik.DDD.Models.Enumerations;
using MartinDrozdik.DDD.Models.Enumerations.Errors;
using MartinDrozdik.DDD.Models.Tests.Enumerations.Assertions;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations;

public class StaticEnumerationTests
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
}
