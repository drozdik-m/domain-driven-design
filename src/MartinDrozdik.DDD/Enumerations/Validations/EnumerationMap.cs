namespace MartinDrozdik.DDD.Enumerations.Validations;

/// <summary>
/// Builds the <see cref="EnumerationMapper{TEnumeration}"/> a validation rule is written against.
/// </summary>
public static class EnumerationMap
{
    /// <summary>
    /// Targets the given <see cref="Enumeration"/> type.
    /// </summary>
    /// <example>
    /// <code>
    /// RuleFor(x => x.State).MustMapToEnumeration(EnumerationMap.To&lt;InvoiceState&gt;());
    /// </code>
    /// </example>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <returns>The mapper for <typeparamref name="TEnumeration"/>.</returns>
    public static EnumerationMapper<TEnumeration> To<TEnumeration>()
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
        => default;
}
