namespace MartinDrozdik.DDD.Results.Exceptions;

/// <summary>
/// Thrown when the error of a successful result is accessed.
/// </summary>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public sealed class ResultSuccessException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultSuccessException"/> class.
    /// </summary>
    public ResultSuccessException()
        : base("You attempted to access the Error property of a successful result. A successful result has no error.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultSuccessException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ResultSuccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultSuccessException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The causing exception.</param>
    public ResultSuccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
