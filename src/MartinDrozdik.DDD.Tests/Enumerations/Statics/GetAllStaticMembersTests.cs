using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Models.Enumerations.Statics;

namespace MartinDrozdik.DDD.Tests.Enumerations.Statics;

public class GetAllStaticMembersTests
{
    [Fact]
    public void Should_return_all_public_static_declared_enumeration_members_of_enumeration()
    {
        // Arrange
        List<TestEnum> expectedMembers = [TestEnum.Value1, TestEnum.Value2, TestEnum.Value3];

        // Act
        var result = EnumerationMembers.GetAllStaticMembers<TestEnum>().ToList();

        // Assert
        Assert.Equal(expectedMembers.Count, result.Count);
        Assert.Equivalent(expectedMembers, result);
    }

    [Fact]
    public void Should_return_empty_list_when_no_static_members_exist()
    {
        // Arrange
        var result = EnumerationMembers.GetAllStaticMembers<EmptyEnum>().ToList();

        // Assert
        Assert.Empty(result);
    }

    private class TestEnum : StaticEnumeration<TestEnum>
    {
        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable S1144 // Unused private types or members should be removed
        public static TestEnum Value3 = new(nameof(Value3));
        public static string ValueString = nameof(ValueString);
        public static int ValueInt = 420;
        public static object ValueObject = new();
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore SA1401 // Fields should be private

        private TestEnum(EnumerationName name)
            : base(name)
        {
        }
    }

#pragma warning disable S3453 // Classes should not have only "private" constructors
    private class EmptyEnum : StaticEnumeration<EmptyEnum>
#pragma warning restore S3453 // Classes should not have only "private" constructors
    {
        private EmptyEnum(EnumerationName name)
            : base(name)
        {
        }
    }
}
