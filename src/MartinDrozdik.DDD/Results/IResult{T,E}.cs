namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Outcome of an operation that returns <typeparamref name="T"/> on success or an <typeparamref name="E"/> error on failure.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
/// <typeparam name="E">Type of the error.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public interface IResult<out T, out E> : IValue<T>, IUnitResult<E>
{
}
