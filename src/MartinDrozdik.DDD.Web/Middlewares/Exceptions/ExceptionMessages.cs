namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// English messages used by the exception handlers when building problem details.
/// </summary>
internal static class ExceptionMessages
{
    /// <summary>
    /// Title of a problem details response describing a general error.
    /// </summary>
    public const string ExceptionTitle = "An error occurred while processing the request.";

    /// <summary>
    /// Title of a problem details response describing a validation error.
    /// </summary>
    public const string ValidationExceptionTitle = "A validation error occurred while processing the request.";

    /// <summary>
    /// General and safe message for unexpected exceptions. Used in production and other non-development environments.
    /// </summary>
    public const string UnexpectedExceptionDetail = "An unexpected server error occurred.";
}
