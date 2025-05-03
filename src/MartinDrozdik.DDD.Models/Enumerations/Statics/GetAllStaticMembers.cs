using System.Reflection;
using CSharpFunctionalExtensions;

namespace MartinDrozdik.DDD.Models.Enumerations.Statics;

/// <summary>
/// Static methods for enumeration members.
/// </summary>
internal static partial class EnumerationMembers
{
    /// <summary>
    /// Get all members of the enumeration.
    /// </summary>
    /// <remarks>
    /// All members means `<i>public static fields</i>` of the enumeration class.
    /// </remarks>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <returns>All members of the enumeration.</returns>
    internal static IEnumerable<TEnumeration> GetAllStaticMembers<TEnumeration>()
        where TEnumeration : Enumeration
    {
        var type = typeof(TEnumeration);
        return type.GetFields(BindingFlags.Public |
                              BindingFlags.Static |
                              BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == type)
            .Select(f => f.GetValue(null))
            .Cast<TEnumeration>();
    }
}
