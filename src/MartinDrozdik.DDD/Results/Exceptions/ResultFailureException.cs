namespace MartinDrozdik.DDD.Results.Exceptions;

/// <summary>
/// Thrown when the value of a failed result is accessed.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public class ResultFailureException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultFailureException"/> class.
    /// </summary>
    public ResultFailureException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultFailureException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ResultFailureException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultFailureException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public ResultFailureException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
