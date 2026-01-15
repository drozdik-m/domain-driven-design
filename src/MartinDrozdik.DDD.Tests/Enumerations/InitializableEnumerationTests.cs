using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Enumerations;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Models.Tests.Enumerations.Assertions;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations;

public class InitializableEnumerationTests
{
    [Fact]
    public void Uninitialized_enum_can_not_operate()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => UninitializedEnum.FromName(nameof(UninitializedEnum.Value1)));
        Assert.Throws<InvalidOperationException>(() => UninitializedEnum.FromNameOptional(nameof(UninitializedEnum.Value1)));
        Assert.Throws<InvalidOperationException>(UninitializedEnum.GetAll);
    }

    [Fact]
    public void Should_initialize_well_known_values_correctly()
    {
        // Act
        TestEnumWithWellKnown.InitializeWellKnown();

        // Assert
        var allStatic = EnumerationMembers.GetAllStaticMembers<TestEnumWithWellKnown>();
        var all = TestEnumWithWellKnown.GetAll();
        Assert.Equivalent(allStatic, all);
    }

    [Fact]
    public void Initial_values_must_contain_well_known_values()
    {
        // Act
        var values = TestEnum.Values.Where(e => e.Name != TestEnum.Value1.Name);

        // Assert
        Assert.Throws<ArgumentException>(() => TestEnum.Initialize(values));
    }

    [Fact]
    public void Should_implement_IEnumerationDeserializer_correctly()
    {
        TestEnum.Initialize(TestEnum.Values);

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
        TestEnum.Initialize(TestEnum.Values);

        // Act & Assert
        EnumerationEnumeratorAssertions.AssertGetAll(TestEnum.Values);
    }

    private class TestEnum(EnumerationName name) : InitializableEnumeration<TestEnum>(name)
    {
        // Well known values
        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));

        public static List<TestEnum> Values { get; } = [
                Value1,
                Value2,
                new ("Value3"),
                new ("Value4")
            ];
    }

    private class TestEnumWithWellKnown(EnumerationName name) : InitializableEnumeration<TestEnumWithWellKnown>(name)
    {
        // Well known values
        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));
    }

    private class UninitializedEnum : InitializableEnumeration<UninitializedEnum>
    {
        // Well known values
        public static readonly TestEnum Value1 = new(nameof(Value1));
        public static readonly TestEnum Value2 = new(nameof(Value2));

        public UninitializedEnum(EnumerationName name)
            : base(name)
        {
        }
    }
}
