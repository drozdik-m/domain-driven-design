namespace MartinDrozdik.DDD.Models.Mediator.Exceptions;

/// <summary>
/// Represents errors that occur within the Mediator framework.
/// </summary>
public class MediatorException : Exception
{
    /// <inheritdoc />
    public MediatorException()
    {
    }

    /// <inheritdoc />
    public MediatorException(string? message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public MediatorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
