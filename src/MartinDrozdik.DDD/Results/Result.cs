namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Creates <see cref="Result{T, E}"/> instances.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public static class Result
{
    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="value">The value of the successful operation.</param>
    /// <returns>A successful <see cref="Result{T, E}"/>.</returns>
    public static Result<T, E> Success<T, E>(T value)
        => new(isFailure: false, value, error: default);

    /// <summary>
    /// Creates a successful result with no value.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <returns>A successful <see cref="UnitResult{E}"/>.</returns>
    public static UnitResult<E> Success<E>()
        => UnitResult.Success<E>();

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="error">The error of the failed operation.</param>
    /// <returns>A failed <see cref="Result{T, E}"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="error"/> is null.</exception>
    public static Result<T, E> Failure<T, E>(E error)
        => new(isFailure: true, value: default, error: error);

    /// <summary>
    /// Creates a failed result with no value.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="error">The error of the failed operation.</param>
    /// <returns>A failed <see cref="UnitResult{E}"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="error"/> is null.</exception>
    public static UnitResult<E> Failure<E>(E error)
        => UnitResult.Failure(error);
}
