namespace MartinDrozdik.DDD.Results.Exceptions;

#pragma warning disable RCS1194 // Implement exception constructors - the error is what builds the message

/// <summary>
/// Thrown when the value of a failed result is accessed.
/// </summary>
/// <typeparam name="E">Type of the error carried by the failed result.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public sealed class ResultFailureException<E> : ResultFailureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultFailureException{E}"/> class.
    /// </summary>
    /// <param name="error">The error carried by the failed result.</param>
    public ResultFailureException(E error)
        : base($"You attempted to access the Value property of a failed result. A failed result has no value. The error was: {error}")
    {
        Error = error;
    }

    /// <summary>
    /// Gets the error in the failed result.
    /// </summary>
    public E Error { get; }
}

#pragma warning restore RCS1194 // Implement exception constructors
