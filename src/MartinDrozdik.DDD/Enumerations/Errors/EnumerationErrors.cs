using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Enumerations.Errors;

/// <summary>
/// Provides a set of predefined service errors.
/// </summary>
public static class EnumerationErrors
{
    /// <summary>
    /// Gets the error that represents a not found error.
    /// </summary>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <param name="name">The value that was not found.</param>
    /// <returns>The <see cref="Error"/> object.</returns>
    public static Error EnumerationNameNotFound<TEnumeration>(EnumerationName name)
        where TEnumeration : Enumeration
    {
        var enumName = typeof(TEnumeration).Name;
        return new ErrorBuilder()
            .WithCode(EnumerationErrorCodes.EnumerationNameNotFound)
            .WithMessage($"Enumeration value {name} not found for {enumName}.")
            .WithDetail("Enumeration", enumName)
            .WithDetail("Name", name.Key)
            .Build();
    }

    /// <summary>
    /// Gets the error that represents an enumeration member with no counterpart in a plain .NET <see cref="Enum"/>.
    /// </summary>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <param name="enumeration">The enumeration member that could not be mapped.</param>
    /// <returns>The <see cref="Error"/> object.</returns>
    public static Error StructEnumMemberNotFound<TEnum>(Enumeration enumeration)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(enumeration);

        var enumerationName = enumeration.GetType().Name;
        var structEnumName = typeof(TEnum).Name;
        return new ErrorBuilder()
            .WithCode(EnumerationErrorCodes.StructEnumMemberNotFound)
            .WithMessage($"Enumeration value {enumeration.Name} of {enumerationName} has no counterpart in {structEnumName}.")
            .WithDetail("Enumeration", enumerationName)
            .WithDetail("StructEnum", structEnumName)
            .WithDetail("Name", enumeration.Name.Key)
            .Build();
    }
}
