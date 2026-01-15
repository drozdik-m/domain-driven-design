using MartinDrozdik.DDD.Errors.WellKnown;

namespace MartinDrozdik.DDD.Models.Errors.WellKnown;

/// <summary>
/// Provides a set of predefined service errors.
/// </summary>
public static class ServiceErrors
{
    private static readonly Error s_notFound = new(ErrorCodes.NotFound, WellKnownErrorsResource.NotFound, []);
    private static readonly Error s_alreadyExists = new(ErrorCodes.AlreadyExists, WellKnownErrorsResource.AlreadyExists, []);

    /// <summary>
    /// Gets the error that represents a not found error.
    /// </summary>
    /// <returns>The <see cref="Error"/> object.</returns>
    public static Error GetNotFound() => s_notFound;

    /// <summary>
    /// Gets the error that represents an already exists error.
    /// </summary>
    /// <returns>The <see cref="Error"/> object.</returns>
    public static Error GetAlreadyExists() => s_alreadyExists;
}
