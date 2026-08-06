using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using MartinDrozdik.DDD.Enumerations.Attributes;

namespace MartinDrozdik.DDD.Enumerations.Statics;

/// <summary>
/// Resolves and caches the <see cref="EnumerationName"/> that each member of a plain .NET <see cref="Enum"/> maps to.
/// </summary>
internal static class StructEnumNames
{
    private static readonly ConcurrentDictionary<Type, FrozenDictionary<string, EnumerationName>> s_cache = new();

    /// <summary>
    /// Gets the <see cref="EnumerationName"/> the given .NET enum value maps to.
    /// </summary>
    /// <remarks>
    /// A value that is not a defined member of its enum type has no name to map, so the raw value is returned instead.
    /// Such a name matches no <see cref="Enumeration"/> member, which makes undefined values behave exactly like defined but unmapped ones.
    /// </remarks>
    /// <param name="value">The .NET enum value.</param>
    /// <returns>The mapped <see cref="EnumerationName"/>.</returns>
    /// <exception cref="ArgumentException">When the enum type cannot be mapped.</exception>
    internal static EnumerationName Resolve(Enum value)
    {
        var type = value.GetType();
        var names = For(type);
        var memberName = Enum.GetName(type, value);

        if (memberName is null || !names.TryGetValue(memberName, out var name))
        {
            return new EnumerationName(value.ToString());
        }

        return name;
    }

    /// <summary>
    /// Gets the cached map of .NET enum member names to <see cref="EnumerationName"/>s.
    /// </summary>
    /// <param name="enumType">Type of the .NET enum.</param>
    /// <returns>Map of member names to <see cref="EnumerationName"/>s.</returns>
    /// <exception cref="ArgumentException">When the enum type cannot be mapped.</exception>
    internal static FrozenDictionary<string, EnumerationName> For(Type enumType)
        => s_cache.GetOrAdd(enumType, Build);

    /// <summary>
    /// Reflects over the .NET enum members and builds the name map.
    /// </summary>
    /// <param name="enumType">Type of the .NET enum.</param>
    /// <returns>Map of member names to <see cref="EnumerationName"/>s.</returns>
    /// <exception cref="ArgumentException">When the enum type cannot be mapped.</exception>
    private static FrozenDictionary<string, EnumerationName> Build(Type enumType)
    {
        // A combination of flags has no single name, so it can never identify one enumeration member
        if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            throw new ArgumentException($"The enum {enumType.Name} is marked with {nameof(FlagsAttribute)}. A combination of flags has no single {nameof(EnumerationName)} and cannot be mapped to an {nameof(Enumeration)}.");
        }

        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        ThrowIfAliasedValues(enumType, fields);

        var members = fields
            .Select(f => (
                Member: f.Name,
                Name: f.GetCustomAttribute<EnumerationNameAttribute>()?.Name ?? new EnumerationName(f.Name)))
            .ToList();

        ThrowIfDuplicateNames(enumType, members);

        return members.ToFrozenDictionary(m => m.Member, m => m.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Throws when several members of the .NET enum share the same underlying value.
    /// </summary>
    /// <remarks>
    /// Aliases are legal C#, but they make the mapping ambiguous, because a single value
    /// would have to map to more than one <see cref="Enumeration"/> member.
    /// </remarks>
    /// <param name="enumType">Type of the .NET enum.</param>
    /// <param name="fields">Members of the .NET enum.</param>
    /// <exception cref="ArgumentException">When aliased values are found.</exception>
    private static void ThrowIfAliasedValues(Type enumType, IEnumerable<FieldInfo> fields)
    {
        var aliases = fields
            .GroupBy(e => e.GetRawConstantValue())
            .Where(e => e.Count() > 1)
            .ToList();

        if (aliases.Count == 0)
        {
            return;
        }

        var offending = string.Join(", ", aliases.Select(g => $"{g.Key} ({string.Join(" = ", g.Select(f => f.Name))})"));
        throw new ArgumentException($"Found {aliases.Count} aliased value(s) in enum {enumType.Name}: {offending}. Aliases make the mapping to an {nameof(Enumeration)} ambiguous.");
    }

    /// <summary>
    /// Throws when several members of the .NET enum map to the same <see cref="EnumerationName"/>.
    /// </summary>
    /// <param name="enumType">Type of the .NET enum.</param>
    /// <param name="members">Members of the .NET enum with their mapped names.</param>
    /// <exception cref="ArgumentException">When duplicate names are found.</exception>
    private static void ThrowIfDuplicateNames(Type enumType, IEnumerable<(string Member, EnumerationName Name)> members)
    {
        var duplicates = members
            .GroupBy(m => m.Name)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
        {
            return;
        }

        var offending = string.Join(", ", duplicates.Select(g => $"{g.Key} ({string.Join(", ", g.Select(m => m.Member))})"));
        throw new ArgumentException($"Found {duplicates.Count} duplicate enumeration name(s) in enum {enumType.Name}: {offending}.");
    }
}
