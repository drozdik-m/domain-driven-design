using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Enumerations;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Tests;

public partial class ValueObjectTests
{
    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var value1 = new TestEqualityValueObject(1, "Test");
        var value2 = new TestEqualityValueObject(1, "Test");
        var differentValue = new TestEqualityValueObject(1, "TestX");

        // Act & Assert
        EqualityAssertions.TestEqualityComparer(comparer: value1, value1, value2, differentValue);
        EqualityAssertions.TestEqualityOperators<Templates.ValueObject>(value1, value2, differentValue);
    }

    private class TestEqualityValueObject(int value1, string value2)
        : Templates.ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return value1;
            yield return value2;
        }
    }
}
