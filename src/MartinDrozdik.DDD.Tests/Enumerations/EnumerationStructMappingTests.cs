using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Enumerations.Attributes;

namespace MartinDrozdik.DDD.Tests.Enumerations;

public class EnumerationStructMappingTests
{
    private enum CompleteState
    {
        One,
        Two,

        [EnumerationName("Three")]
        Third,
    }

    private enum MissingMemberState
    {
        One,
        Two,
    }

    private enum ExtraMemberState
    {
        One,
        Two,
        Three,
        Four,
    }

    private enum MismatchedState
    {
        One,
        Two,
        Surplus,
    }

    [Flags]
    private enum FlagsState
    {
        None = 0,
        One = 1,
        Two = 2,
    }

    private enum AliasedState
    {
        One = 1,
        Two = 2,
        Three = 3,
        Duplicate = 3,
    }

    private enum UninitializedState
    {
        One,
    }

    [Fact]
    public void ThrowIfIncomplete_passes_for_a_complete_mapping()
    {
        // Act
        var exception = Record.Exception(EnumerationStructMapping.ThrowIfIncomplete<TestEnum, CompleteState>);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowIfIncomplete_throws_for_an_enumeration_member_without_a_counterpart()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<TestEnum, MissingMemberState>);

        // Assert
        Assert.Contains($"Unmapped {nameof(TestEnum)} member(s): {nameof(TestEnum.Three)}", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain($"Unmapped {nameof(MissingMemberState)}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_throws_for_a_struct_enum_member_without_a_counterpart()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<TestEnum, ExtraMemberState>);

        // Assert
        Assert.Contains($"Unmapped {nameof(ExtraMemberState)} member(s): {nameof(ExtraMemberState.Four)}", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain($"Unmapped {nameof(TestEnum)}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_reports_both_sides_when_both_have_unmapped_members()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<TestEnum, MismatchedState>);

        // Assert
        Assert.Contains($"Unmapped {nameof(TestEnum)} member(s): {nameof(TestEnum.Three)}", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"Unmapped {nameof(MismatchedState)} member(s): {nameof(MismatchedState.Surplus)}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_names_the_mapped_name_of_a_renamed_member()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<SingleMemberEnum, CompleteState>);

        // Assert
        Assert.Contains($"{nameof(CompleteState.Third)} (mapped to Three)", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_throws_for_a_flags_enum()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<TestEnum, FlagsState>);

        // Assert
        Assert.Contains(nameof(FlagsAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_throws_for_an_enum_with_aliased_values()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            EnumerationStructMapping.ThrowIfIncomplete<TestEnum, AliasedState>);

        // Assert
        Assert.Contains(nameof(AliasedState.Duplicate), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfIncomplete_throws_for_an_uninitialized_enumeration()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            EnumerationStructMapping.ThrowIfIncomplete<UninitializedEnum, UninitializedState>);
    }

    private class TestEnum(EnumerationName name) : StaticEnumeration<TestEnum>(name)
    {
        public static readonly TestEnum One = new(nameof(One));
        public static readonly TestEnum Two = new(nameof(Two));
        public static readonly TestEnum Three = new(nameof(Three));
    }

    private class SingleMemberEnum(EnumerationName name) : StaticEnumeration<SingleMemberEnum>(name)
    {
        public static readonly SingleMemberEnum One = new(nameof(One));
        public static readonly SingleMemberEnum Two = new(nameof(Two));
    }

    private class UninitializedEnum(EnumerationName name) : InitializableEnumeration<UninitializedEnum>(name)
    {
        public static readonly UninitializedEnum One = new(nameof(One));
    }
}
