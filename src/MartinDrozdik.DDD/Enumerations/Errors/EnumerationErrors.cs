using MartinDrozdik.DDD.Enumerations.Errors;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations.Errors;

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
            .WithMessage(string.Format(EnumerationErrorsResource.EnumerationNameNotFoundError, name, name))
            .WithDetail("Enumeration", enumName)
            .WithDetail("Name", name.Key)
            .Build();
    }
}
