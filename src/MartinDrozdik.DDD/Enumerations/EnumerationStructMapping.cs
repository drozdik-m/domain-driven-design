using System.Text;
using MartinDrozdik.DDD.Enumerations.Attributes;
using MartinDrozdik.DDD.Enumerations.Statics;

namespace MartinDrozdik.DDD.Enumerations;

/// <summary>
/// Verifies that an <see cref="Enumeration"/> and a plain .NET <see cref="Enum"/> map to each other one to one.
/// </summary>
/// <remarks>
/// <see cref="EnumerationStructExtensions.ToStructEnum{TEnum}(Enumeration)"/> and
/// <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}(TEnum)"/> throw when a member has no
/// counterpart, which surfaces a broken mapping contract only once production reaches that member.
/// This class turns that into an up-front check.
/// </remarks>
public static class EnumerationStructMapping
{
    /// <summary>
    /// Throws unless every <typeparamref name="TEnumeration"/> member has a <typeparamref name="TEnum"/> counterpart and the other way round.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call it at startup or from a test. Once it passes, neither
    /// <see cref="EnumerationStructExtensions.ToStructEnum{TEnum}(Enumeration)"/> nor
    /// <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}(TEnum)"/> should fail for a defined member.
    /// </para>
    /// <para>
    /// Members are matched by name, so <see cref="EnumerationNameAttribute"/> is honoured.
    /// An <see cref="InitializableEnumeration{TSelf}"/> must be initialized first, because the check lists its members.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// EnumerationStructMapping.ThrowIfIncomplete&lt;InvoiceState, InvoiceStateDto&gt;();
    /// </code>
    /// </example>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <exception cref="ArgumentException">When the two types do not map one to one, or when the .NET enum type cannot be mapped at all.</exception>
    /// <exception cref="InvalidOperationException">When the enumeration has not been initialized.</exception>
    public static void ThrowIfIncomplete<TEnumeration, TEnum>()
        where TEnumeration : Enumeration, IEnumerationEnumerator<TEnumeration>
        where TEnum : struct, Enum
    {
        // Building the map also rejects flag enums, aliased values and duplicate names
        var structMembers = StructEnumMap<TEnum>.ByName;
        var enumerationMembers = TEnumeration.GetAll().ToList();
        var enumerationNames = enumerationMembers.Select(e => e.Name).ToHashSet();

        var unmappedEnumerationMembers = enumerationMembers
            .Where(member => !structMembers.ContainsKey(member.Name))
            .Select(member => member.Name.Key)
            .ToList();

        var unmappedStructMembers = structMembers
            .Where(pair => !enumerationNames.Contains(pair.Key))
            .Select(pair => Describe(pair.Value, pair.Key))
            .ToList();

        if (unmappedEnumerationMembers.Count == 0 && unmappedStructMembers.Count == 0)
        {
            return;
        }

        throw new ArgumentException(BuildMessage<TEnumeration, TEnum>(unmappedEnumerationMembers, unmappedStructMembers));
    }

    /// <summary>
    /// Describes a .NET enum member by the name of its field, and by the name it maps to when the two differ.
    /// </summary>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="member">The .NET enum member.</param>
    /// <param name="name">The <see cref="EnumerationName"/> the member maps to.</param>
    /// <returns>The description of the member.</returns>
    private static string Describe<TEnum>(TEnum member, EnumerationName name)
        where TEnum : struct, Enum
    {
        var memberName = member.ToString();
        return memberName == name.Key
            ? memberName
            : $"{memberName} (mapped to {name.Key})";
    }

    /// <summary>
    /// Builds the message listing the members that have no counterpart.
    /// </summary>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="unmappedEnumerationMembers">Enumeration members with no .NET enum counterpart.</param>
    /// <param name="unmappedStructMembers">.NET enum members with no enumeration counterpart.</param>
    /// <returns>The message.</returns>
    private static string BuildMessage<TEnumeration, TEnum>(
        List<string> unmappedEnumerationMembers,
        List<string> unmappedStructMembers)
        where TEnumeration : Enumeration
        where TEnum : struct, Enum
    {
        var enumerationName = typeof(TEnumeration).Name;
        var structEnumName = typeof(TEnum).Name;

        var message = new StringBuilder()
            .Append($"Enumeration {enumerationName} and enum {structEnumName} do not map 1:1.");

        if (unmappedEnumerationMembers.Count > 0)
        {
            message.AppendLine()
                .Append($"  Unmapped {enumerationName} member(s): {string.Join(", ", unmappedEnumerationMembers)}");
        }

        if (unmappedStructMembers.Count > 0)
        {
            message.AppendLine()
                .Append($"  Unmapped {structEnumName} member(s): {string.Join(", ", unmappedStructMembers)}");
        }

        return message.ToString();
    }
}
