using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations.Errors;

/// <summary>
/// Provides a set of predefined error codes.
/// </summary>
public static class EnumerationErrorCodes
{
    /// <summary>
    /// Gets the error code meaning that the enumeration member was not found.
    /// </summary>
    public static ErrorCode EnumerationNameNotFound { get; } = new ErrorCode(nameof(EnumerationNameNotFound));
}
