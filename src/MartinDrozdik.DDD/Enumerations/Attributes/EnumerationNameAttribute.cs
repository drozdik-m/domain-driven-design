namespace MartinDrozdik.DDD.Enumerations.Attributes;

/// <summary>
/// Overrides the <see cref="EnumerationName"/> a plain .NET <see cref="Enum"/> member maps to.
/// </summary>
/// <remarks>
/// By default a .NET enum member maps to the <see cref="Enumeration"/> member of the same name.
/// Apply this attribute when the two names must differ, for example when:
/// <list type="bullet">
///     <item>The domain name is not valid C# identifier</item>
///     <item>Legacy API constrains</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public enum InvoiceStateDto
/// {
///     Draft,
///     Issued,
///
///     [EnumerationName("Paid")]
///     Settled,
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the <see cref="EnumerationNameAttribute"/> class.
/// </remarks>
/// <param name="name">Name of the <see cref="Enumeration"/> member this .NET enum member maps to.</param>
/// <exception cref="ArgumentNullException">When the name is null, empty or whitespace.</exception>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class EnumerationNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the <see cref="Enumeration"/> member this .NET enum member maps to.
    /// </summary>
    public EnumerationName Name { get; } = new EnumerationName(name);
}
