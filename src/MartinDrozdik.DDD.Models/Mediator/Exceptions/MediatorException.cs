namespace MartinDrozdik.DDD.Models.Mediator.Exceptions;

internal class MediatorException : Exception
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
