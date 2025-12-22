namespace MartinDrozdik.DDD.Models.Mediator.Exceptions;

/// <summary>
/// Represents errors that occur within the Mediator framework.
/// </summary>
public class MediatorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorException"/> class.
    /// </summary>
    public MediatorException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public MediatorException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public MediatorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
