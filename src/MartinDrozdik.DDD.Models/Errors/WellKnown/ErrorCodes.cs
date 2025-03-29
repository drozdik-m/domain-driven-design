namespace MartinDrozdik.DDD.Models.Errors.WellKnown;

/// <summary>
/// Provides a set of predefined error codes.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Gets the error code that represents a not found error.
    /// </summary>
    public static ErrorCode NotFound { get; } = new ErrorCode(nameof(NotFound));

    /// <summary>
    /// Gets the error code that represents an already exists error.
    /// </summary>
    public static ErrorCode AlreadyExists { get; } = new ErrorCode(nameof(AlreadyExists));
}
