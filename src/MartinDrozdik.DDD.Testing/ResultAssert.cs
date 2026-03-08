using CSharpFunctionalExtensions;
using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Asserts for result objects.
/// </summary>
public static class ResultAssert
{
    /// <summary>
    /// Asserts that <see cref="UnitResult{E}"/> is a success.
    /// </summary>
    /// <typeparam name="E">Type of the result errror.</typeparam>
    /// <param name="result">The result to check.</param>
    public static void IsSuccess<E>(this UnitResult<E> result)
    {
        Assert.True(result.IsSuccess, $"Expected {nameof(UnitResult<>)} to be successful, but it was not.");
    }

    /// <summary>
    /// Asserts that <see cref="Result{T, E}"/> is a success.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to check.</param>
    public static void IsSuccess<T, E>(this Result<T, E> result)
    {
        Assert.True(result.IsSuccess, $"Expected {nameof(Result<,>)} result to be successful, but it was not.");
    }
}
