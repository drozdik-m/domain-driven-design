using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Exceptions;

/// <summary>
/// Represents errors that occured within business rules.
/// </summary>
public class BusinessRuleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    public BusinessRuleException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BusinessRuleException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Inner exception if any.</param>
    public BusinessRuleException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the details of the business rule violation.
    /// </summary>
    public IEnumerable<ExceptionDetail> Details { get; init; } = [];
}
