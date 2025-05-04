using MartinDrozdik.DDD.Models.Enumerations.Statics;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using MartinDrozdik.DDD.Models.Enumerations;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Statics;

public class GetAllStaticMembersTests
{
    private class TestEnum : StaticEnumeration<TestEnum>
    {
        private TestEnum(EnumerationName name) : base(name)
        {
        }

        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
        public static TestEnum Value3 = new(nameof(Value3));
        public static string ValueString = nameof(ValueString);
        public static int ValueInt = 420;
        public static object ValueObject = new();
    }

    private class EmptyEnum : StaticEnumeration<EmptyEnum>
    {
        private EmptyEnum(EnumerationName name) : base(name)
        {
        }
    }

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
}
