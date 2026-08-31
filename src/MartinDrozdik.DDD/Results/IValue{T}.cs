namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Has the value produced by a successful operation.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public interface IValue<out T>
{
    /// <summary>
    /// Gets the value of a successful operation.
    /// </summary>
    /// <exception cref="Exceptions.ResultFailureException{E}">When the operation failed.</exception>
    T Value { get; }
}
