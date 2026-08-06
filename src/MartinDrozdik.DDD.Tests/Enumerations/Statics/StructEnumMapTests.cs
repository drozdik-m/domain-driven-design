using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Enumerations.Attributes;
using MartinDrozdik.DDD.Enumerations.Statics;

namespace MartinDrozdik.DDD.Tests.Enumerations.Statics;

public class StructEnumMapTests
{
    private enum TestState
    {
        One,
        Two,

        [EnumerationName("Three")]
        Third,
    }

    [Flags]
    private enum FlagsState
    {
        None = 0,
        First = 1,
        Second = 2,
    }

    private enum DuplicateNamesState
    {
        First,

        [EnumerationName(nameof(First))]
        Second,
    }

    private enum AliasedState
    {
        First = 1,
        Alias = 1,
    }

    private enum EmptyState
    {
    }

    [Fact]
    public void Should_map_struct_enum_members_by_their_names()
    {
        // Act
        var result = StructEnumMap<TestState>.ByName;

        // Assert
        Assert.Equal(TestState.One, result[new EnumerationName(nameof(TestState.One))]);
        Assert.Equal(TestState.Two, result[new EnumerationName(nameof(TestState.Two))]);
    }

    [Fact]
    public void Should_honor_the_enumeration_name_attribute()
    {
        // Act
        var result = StructEnumMap<TestState>.ByName;

        // Assert
        Assert.Equal(TestState.Third, result[new EnumerationName("Three")]);
        Assert.DoesNotContain(new EnumerationName(nameof(TestState.Third)), result.Keys);
    }

    [Fact]
    public void Should_return_the_same_map_on_repeated_access()
    {
        // Act
        var first = StructEnumMap<TestState>.ByName;
        var second = StructEnumMap<TestState>.ByName;

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Should_throw_for_a_flags_enum()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => StructEnumMap<FlagsState>.ByName);

        // Assert
        Assert.Contains(nameof(FlagsAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_throw_for_duplicate_enumeration_names()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => StructEnumMap<DuplicateNamesState>.ByName);

        // Assert
        Assert.Contains(nameof(DuplicateNamesState.First), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DuplicateNamesState.Second), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_throw_for_aliased_values()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => StructEnumMap<AliasedState>.ByName);

        // Assert
        Assert.Contains(nameof(AliasedState.First), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(AliasedState.Alias), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_return_an_empty_map_for_an_empty_enum()
    {
        // Act
        var result = StructEnumMap<EmptyState>.ByName;

        // Assert
        Assert.Empty(result);
    }
}
