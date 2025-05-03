namespace MartinDrozdik.DDD.Models.Enumerations;

/// <summary>
/// An enumeration that can list all its members.
/// </summary>
/// <typeparam name="TEnumeration">The of the enumeration.</typeparam>
public interface IEnumerationEnumerator<out TEnumeration>
    where TEnumeration : Enumeration
{
    /// <summary>
    /// Returns all enumeration members.
    /// </summary>
    /// <returns>All enumeration members.</returns>
    static abstract IEnumerable<TEnumeration> GetAll();
}
