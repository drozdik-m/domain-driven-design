using CSharpFunctionalExtensions;

namespace MartinDrozdik.DDD.Models.Enumerations.Statics;

/// <summary>
/// Static methods for enumeration members.
/// </summary>
internal static partial class EnumerationMembers
{
    /// <summary>
    /// If the enumeration members contain duplicates, an exception is thrown.
    /// </summary>
    /// <exception cref="ArgumentException">When duplicate values are found.</exception>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <param name="values">List of enumeration members.</param>
    internal static void ThrowIfDuplicateMembers<TEnumeration>(this IEnumerable<TEnumeration> values)
        where TEnumeration : Enumeration
    {
        var duplicates = values
            .GroupBy(e => e.Name)
            .Where(e => e.Count() > 1)
            .ToList();

        // No duplicates found
        if (duplicates.Count == 0)
        {
            return;
        }

        // Duplicates found
        var duplicatesCount = duplicates.Select(e => (e.First(), e.Count()));
        throw new ArgumentException($"Found {duplicatesCount} duplicate enumeration members.");
    }
}
