namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Outcome of an operation that succeeded or failed.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }
}
