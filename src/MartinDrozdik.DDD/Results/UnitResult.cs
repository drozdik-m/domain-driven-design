namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Creates <see cref="UnitResult{E}"/> instances.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public static class UnitResult
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <returns>A successful <see cref="UnitResult{E}"/>.</returns>
    public static UnitResult<E> Success<E>()
        => default;

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="error">The error of the failed operation.</param>
    /// <returns>A failed <see cref="UnitResult{E}"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="error"/> is null.</exception>
    public static UnitResult<E> Failure<E>(E error)
        => new(isFailure: true, error);
}
