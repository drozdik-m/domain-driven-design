using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations.Statics;

/// <summary>
/// Static methods for enumeration members.
/// </summary>
internal static partial class EnumerationMembers
{
    /// <summary>
    /// Deserialize the enumeration member from its name.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type.</typeparam>
    /// <param name="name">Serialized value of the enumeration.</param>
    /// <returns>The enumeration if found, else <see cref="Error"/>.</returns>
    internal static Result<TEnumeration?, Error> FromNameOptional<TEnumeration>(EnumerationName? name)
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        // If the name is null, return null
        if (name is null)
        {
            return Result.Success<TEnumeration?, Error>(null);
        }

        // Try to get the enumeration member by name
        var result = TEnumeration.FromName(name.Value);
        return result.IsSuccess
            ? Result.Success<TEnumeration?, Error>(result.Value)
            : Result.Failure<TEnumeration?, Error>(result.Error);
    }
}
