using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Templates.Errors;

/// <summary>
/// Provides a set of predefined error codes for templates.
/// </summary>
public static class TemplateErrorCodes
{
    /// <summary>
    /// Gets the error code that represents an object can not be created to a valid state.
    /// </summary>
    public static ErrorCode InvalidObject { get; } = new ErrorCode(nameof(InvalidObject));
}
