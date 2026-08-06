using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Models.Enumerations.Statics;

/// <summary>
/// Static methods for enumeration members.
/// </summary>
internal static partial class EnumerationMembers
{
    /// <summary>
    /// Deserialize the enumeration member from a plain .NET <see cref="Enum"/> member of the same name.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type.</typeparam>
    /// <param name="value">The .NET enum member to convert.</param>
    /// <returns>The matching enumeration member.</returns>
    /// <exception cref="ArgumentException">When the .NET enum type cannot be mapped.</exception>
    /// <exception cref="BusinessRuleException">When no enumeration member matches the .NET enum member.</exception>
    internal static TEnumeration FromStructEnum<TEnumeration>(Enum value)
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        var result = TEnumeration.FromName(value.ToEnumerationName());
        if (result.IsFailure)
        {
            throw result.Error.ToBusinessRuleException();
        }

        return result.Value;
    }
}
