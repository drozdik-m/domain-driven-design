using MartinDrozdik.DDD.Exceptions;

namespace MartinDrozdik.DDD.Enumerations;

/// <summary>
/// Deserializes enumeration members from a plain .NET <see cref="Enum"/> member.
/// </summary>
/// <remarks>
/// Members are matched by name, so an implementation is expected to be an
/// <see cref="IEnumerationDeserializer{TEnumeration}"/> as well.
/// </remarks>
/// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
public interface IStructEnumDeserializer<out TEnumeration>
    where TEnumeration : Enumeration
{
    /// <summary>
    /// Deserializes enumeration member from a plain .NET <see cref="Enum"/> member.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IEnumerationDeserializer{TEnumeration}.FromName(EnumerationName)"/>, which reports a missing
    /// member as a failed result, this method throws. A .NET enum without a counterpart here is a broken mapping
    /// contract, that is a bug rather than a business failure.
    /// </remarks>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="value">The .NET enum member to convert.</param>
    /// <returns>The matching enumeration member.</returns>
    /// <exception cref="BusinessRuleException">When no enumeration member matches the .NET enum member.</exception>
    static abstract TEnumeration FromStructEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum;

    /// <summary>
    /// Deserializes enumeration member from an optional plain .NET <see cref="Enum"/> member.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IEnumerationDeserializer{TEnumeration}.FromNameOptional(EnumerationName?)"/>, which reports
    /// a missing member as a failed result, this method throws. A .NET enum without a counterpart here is a broken
    /// mapping contract, that is a bug rather than a business failure.
    /// </remarks>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="value">The .NET enum member to convert, or null.</param>
    /// <returns>The matching enumeration member, or null when the value is null.</returns>
    /// <exception cref="BusinessRuleException">When no enumeration member matches the .NET enum member.</exception>
    static abstract TEnumeration? FromStructEnumOptional<TEnum>(TEnum? value)
        where TEnum : struct, Enum;
}
