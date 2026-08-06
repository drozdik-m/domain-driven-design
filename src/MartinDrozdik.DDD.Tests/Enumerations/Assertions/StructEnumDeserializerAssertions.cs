using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Exceptions;

namespace MartinDrozdik.DDD.Tests.Enumerations.Assertions;

/// <summary>
/// Assertions for <see cref="IStructEnumDeserializer{TEnumeration}"/> implementations.
/// </summary>
public static class StructEnumDeserializerAssertions
{
    /// <summary>
    /// Asserts that both <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}"/> and <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnumOptional{TEnum}"/> implementations work as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IStructEnumDeserializer{TEnumeration}"/>.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="validValue">A .NET enum member that has a matching enumeration member.</param>
    /// <param name="invalidValue">A .NET enum member that has no matching enumeration member.</param>
    /// <param name="expectedValue">The expected enumeration member for the valid value.</param>
    public static void AssertStructEnumDeserializer<TEnumeration, TEnum>(
        TEnum validValue,
        TEnum invalidValue,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IStructEnumDeserializer<TEnumeration>
        where TEnum : struct, Enum
    {
        AssertFromStructEnum(validValue, invalidValue, expectedValue);
        AssertFromStructEnumOptional(validValue, invalidValue, expectedValue);
    }

    /// <summary>
    /// Asserts that the <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}"/> implementation works as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IStructEnumDeserializer{TEnumeration}"/>.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="validValue">A .NET enum member that has a matching enumeration member.</param>
    /// <param name="invalidValue">A .NET enum member that has no matching enumeration member.</param>
    /// <param name="expectedValue">The expected enumeration member for the valid value.</param>
    public static void AssertFromStructEnum<TEnumeration, TEnum>(
        TEnum validValue,
        TEnum invalidValue,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IStructEnumDeserializer<TEnumeration>
        where TEnum : struct, Enum
    {
        // Valid value
        var result = TEnumeration.FromStructEnum(validValue);
        Assert.Equal(expectedValue, result);

        // Invalid value
        var exception = Record.Exception(() => TEnumeration.FromStructEnum(invalidValue));
        Assert.True(exception is BusinessRuleException, $"{nameof(IStructEnumDeserializer<>.FromStructEnum)} did not throw a {nameof(BusinessRuleException)} for invalid value '{invalidValue}'.");
    }

    /// <summary>
    /// Asserts that the <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnumOptional{TEnum}"/> implementation works as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IStructEnumDeserializer{TEnumeration}"/>.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="validValue">A .NET enum member that has a matching enumeration member.</param>
    /// <param name="invalidValue">A .NET enum member that has no matching enumeration member.</param>
    /// <param name="expectedValue">The expected enumeration member for the valid value.</param>
    public static void AssertFromStructEnumOptional<TEnumeration, TEnum>(
        TEnum validValue,
        TEnum invalidValue,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IStructEnumDeserializer<TEnumeration>
        where TEnum : struct, Enum
    {
        // Valid value
        var result = TEnumeration.FromStructEnumOptional<TEnum>(validValue);
        Assert.Equal(expectedValue, result);

        // Invalid value
        var exception = Record.Exception(() => TEnumeration.FromStructEnumOptional<TEnum>(invalidValue));
        Assert.True(exception is BusinessRuleException, $"{nameof(IStructEnumDeserializer<>.FromStructEnumOptional)} did not throw a {nameof(BusinessRuleException)} for invalid value '{invalidValue}'.");

        // Null value
        var nullResult = TEnumeration.FromStructEnumOptional<TEnum>(null);
        Assert.Null(nullResult);
    }
}
