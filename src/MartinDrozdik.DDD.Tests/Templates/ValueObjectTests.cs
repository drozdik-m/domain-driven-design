using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Tests.Templates;

public class ValueObjectTests
{
    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var value1 = new TestEqualityValueObject(1, "Test");
        var value2 = new TestEqualityValueObject(1, "Test");
        var differentValue = new TestEqualityValueObject(1, "TestX");

        // Act & Assert
        EqualityAssert.TestEqualityComparer(comparer: value1, value1, value2, differentValue);
        EqualityAssert.TestEqualityOperators<ValueObject>(value1, value2, differentValue);
    }

    private class TestEqualityValueObject(int value1, string value2)
        : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return value1;
            yield return value2;
        }
    }
}
