using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Models.Enumerations;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Statics;

public class ThrowIfDuplicateMembersTests
{
    private class TestEnum : StaticEnumeration<TestEnum>
    {
        private TestEnum(EnumerationName name) : base(name)
        {
        }

        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
        public static readonly TestEnum DuplicateValue1 = new(nameof(Value1)); // Duplicate of Value1
    }

    [Fact]
    public void Should_not_throw_exception_when_no_duplicates_exist()
    {
        // Arrange
        var values = new List<TestEnum> { TestEnum.Value1, TestEnum.Value2 };

        // Act & Assert
        var exception = Record.Exception(() => values.ThrowIfDuplicateMembers());
        Assert.Null(exception);
    }

    [Fact]
    public void Should_throw_exception_when_duplicates_exist()
    {
        // Arrange
        List<TestEnum> values = [TestEnum.Value1, TestEnum.DuplicateValue1, TestEnum.Value2];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => values.ThrowIfDuplicateMembers());
    }
}
