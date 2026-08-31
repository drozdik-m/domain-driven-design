namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Outcome of an operation that returns no value on success and an error on failure.
/// </summary>
/// <typeparam name="E">Type of the error.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public interface IUnitResult<out E> : IResult, IError<E>
{
}
