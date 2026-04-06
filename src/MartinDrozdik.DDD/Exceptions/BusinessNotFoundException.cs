namespace MartinDrozdik.DDD.Exceptions;

/// <summary>
/// Represents errors that occured within business rules where something was not found.
/// </summary>
public class BusinessNotFoundException : BusinessRuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessNotFoundException"/> class.
    /// </summary>
    public BusinessNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BusinessNotFoundException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BusinessNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
