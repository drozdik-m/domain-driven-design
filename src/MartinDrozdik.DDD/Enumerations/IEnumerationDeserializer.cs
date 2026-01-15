using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations;

/// <summary>
/// Deserializes enumeration members from a name ID.
/// </summary>
/// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
public interface IEnumerationDeserializer<out TEnumeration>
    where TEnumeration : Enumeration
{
    /// <summary>
    /// Deserializes enumeration member from a name.
    /// </summary>
    /// <param name="name">Name (ID) of the enumeration.</param>
    /// <returns>Enumeration member if found, otherwise an error.</returns>
    static abstract IResult<TEnumeration, Error> FromName(EnumerationName name);

    /// <summary>
    /// Deserializes enumeration member from a name.
    /// </summary>
    /// <param name="name">Name (ID) of the enumeration.</param>
    /// <returns>If the param is null, null is returned. Enumeration member if found, otherwise null.</returns>
    static abstract IResult<TEnumeration?, Error> FromNameOptional(EnumerationName? name);
}
