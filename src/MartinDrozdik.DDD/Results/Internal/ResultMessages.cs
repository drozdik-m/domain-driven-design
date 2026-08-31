namespace MartinDrozdik.DDD.Results.Internal;

/// <summary>
/// Common messages of the exceptions.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
internal static class ResultMessages
{
    /// <summary>
    /// Message used when a failure is created without an error.
    /// </summary>
    internal const string ErrorIsNotProvidedForFailure =
        "You attempted to create a failed result, which must carry an error, but no error was provided.";
}
