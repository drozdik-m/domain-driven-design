namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Has the error produced by a failed operation.
/// </summary>
/// <typeparam name="E">Type of the error.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public interface IError<out E>
{
    /// <summary>
    /// Gets the error of a failed operation.
    /// </summary>
    /// <exception cref="Exceptions.ResultSuccessException">For successful results.</exception>
    E Error { get; }
}
