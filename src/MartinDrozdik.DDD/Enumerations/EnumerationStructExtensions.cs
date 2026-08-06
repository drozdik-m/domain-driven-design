using MartinDrozdik.DDD.Enumerations.Attributes;
using MartinDrozdik.DDD.Enumerations.Errors;
using MartinDrozdik.DDD.Enumerations.Statics;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Enumerations;

/// <summary>
/// Converts an <see cref="Enumeration"/> to a plain .NET <see cref="Enum"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="Enumeration"/> is a domain type and should not be exposed on a public API.
/// The usual answer is a plain .NET enum for the API contract plus a hand-written mapping method, which has to be kept in sync by hand.
/// These extensions replace that method.
/// The opposite direction lives on the enumeration itself, as <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}(TEnum)"/>.
/// </para>
/// <para>
/// Members are matched by name: <see cref="EnumerationName"/> against the .NET enum member name, case sensitively.
/// Use <see cref="EnumerationNameAttribute"/> when the two names must differ.
/// </para>
/// <para>
/// A member with no counterpart on the other side is a broken mapping contract, that is a bug rather than a business failure, so it throws instead of returning a <c>Result</c>.
/// Guard against it with a test asserting the two member sets match, or, for values arriving from the outside, with the <c>MustMapToEnumeration</c> validation rule.
/// </para>
/// </remarks>
public static class EnumerationStructExtensions
{
    /// <summary>
    /// Converts an <see cref="Enumeration"/> member to the .NET enum member of the same name.
    /// </summary>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="enumeration">The enumeration member to convert.</param>
    /// <returns>The matching .NET enum member.</returns>
    /// <exception cref="ArgumentNullException">When the enumeration is null.</exception>
    /// <exception cref="ArgumentException">When the .NET enum type cannot be mapped.</exception>
    /// <exception cref="BusinessRuleException">When no .NET enum member matches the enumeration member.</exception>
    public static TEnum ToStructEnum<TEnum>(this Enumeration enumeration)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(enumeration);

        if (!StructEnumMap<TEnum>.ByName.TryGetValue(enumeration.Name, out var value))
        {
            throw EnumerationErrors.StructEnumMemberNotFound<TEnum>(enumeration)
                .ToBusinessRuleException();
        }

        return value;
    }

    /// <summary>
    /// Converts an optional <see cref="Enumeration"/> member to the .NET enum member of the same name.
    /// </summary>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="enumeration">The enumeration member to convert, or null.</param>
    /// <returns>The matching .NET enum member, or null when the enumeration is null.</returns>
    /// <exception cref="ArgumentException">When the .NET enum type cannot be mapped.</exception>
    /// <exception cref="BusinessRuleException">When no .NET enum member matches the enumeration member.</exception>
    public static TEnum? ToStructEnumOptional<TEnum>(this Enumeration? enumeration)
        where TEnum : struct, Enum
        => enumeration?.ToStructEnum<TEnum>();

    /// <summary>
    /// Gets the <see cref="EnumerationName"/> a .NET enum member maps to.
    /// </summary>
    /// <remarks>
    /// Honours <see cref="EnumerationNameAttribute"/>.
    /// A value that is not a defined member of its enum type has no name to map, so the raw value is returned instead.
    /// </remarks>
    /// <param name="value">The .NET enum member.</param>
    /// <returns>The mapped <see cref="EnumerationName"/>.</returns>
    /// <exception cref="ArgumentNullException">When the value is null.</exception>
    /// <exception cref="ArgumentException">When the .NET enum type cannot be mapped.</exception>
    public static EnumerationName ToEnumerationName(this Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return StructEnumNames.Resolve(value);
    }
}
