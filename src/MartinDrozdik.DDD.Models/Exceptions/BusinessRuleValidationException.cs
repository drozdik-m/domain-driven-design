namespace MartinDrozdik.DDD.Models.Exceptions;

/// <summary>
/// Represents errors that occured within business rules.
/// </summary>
public class BusinessRuleValidationException : BusinessRuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> class.
    /// </summary>
    public BusinessRuleValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BusinessRuleValidationException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BusinessRuleValidationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
