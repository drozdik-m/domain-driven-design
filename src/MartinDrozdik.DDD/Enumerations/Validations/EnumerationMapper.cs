namespace MartinDrozdik.DDD.Enumerations.Validations;

/// <summary>
/// Carries the <see cref="Enumeration"/> type a plain .NET <see cref="Enum"/> is validated against.
/// </summary>
/// <remarks>
/// C# infers generic arguments all or nothing, so a validation rule generic in the validated object,
/// the .NET enum and the enumeration would have to spell out all three at the call site.
/// Passing the enumeration type as this stateless argument, built by <see cref="EnumerationMap.To{TEnumeration}"/>,
/// lets every argument be inferred instead.
/// </remarks>
/// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
public readonly record struct EnumerationMapper<TEnumeration>
    where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
{
    /// <summary>
    /// Checks whether the given .NET enum member has a matching <typeparamref name="TEnumeration"/> member.
    /// </summary>
    /// <param name="value">The .NET enum member to check.</param>
    /// <returns>True when a matching enumeration member exists, else false.</returns>
#pragma warning disable CA1822 // Mark members as static
    internal bool CanMap(Enum value)
#pragma warning restore CA1822 // Mark members as static
        => TEnumeration.FromName(value.ToEnumerationName()).IsSuccess;
}
