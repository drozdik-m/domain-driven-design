using System.Diagnostics;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Enumerations;

/// <summary>
/// Base class for enumeration types.
/// </summary>
/// <remarks>
/// Enumeration types are a set of unique values that are used to represent a set of named constants.
/// Enumeration classes are more flexible than enums, and they can have methods and properties.
/// </remarks>
[DebuggerDisplay("{Name}")]
public abstract class Enumeration : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Enumeration"/> class.
    /// </summary>
    /// <param name="name">Name of the enumeration.</param>
    protected Enumeration(EnumerationName name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name (ID) of enumeration.
    /// Is unique across all members of enumeration types.
    /// </summary>
    public EnumerationName Name { get; }

    /// <summary>
    /// Implicitly convert an enumeration to a name string.
    /// </summary>
    /// <param name="enumeration">The enumeration to convert.</param>
    public static implicit operator string(Enumeration enumeration)
    {
        return enumeration.Name.Key;
    }

    /// <summary>
    /// Returns <see cref="Name"/> of the enumeration.
    /// </summary>
    /// <returns><see cref="Name"/> as string.</returns>
    public override string ToString() => Name.ToString();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
    }
}
