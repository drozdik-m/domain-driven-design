using System.Diagnostics;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Errors;

/// <summary>
/// Represents an error that occurred during the execution of the application.
/// </summary>
[DebuggerDisplay("{Code.Key}: {Message}")]
public class Error : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="details">Details regarding this error.</param>
    public Error(ErrorCode code, string message, IEnumerable<ErrorDetail> details)
    {
        Code = code;
        Message = message;
        Details = details.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="details">Details regarding this error.</param>
    /// <param name="exception">The causing exception, if any.</param>
    public Error(ErrorCode code, string message, IEnumerable<ErrorDetail> details, Exception? exception)
        : this(code, message, details)
    {
        Exception = exception;
    }

    /// <summary>
    /// Gets the error code.
    /// Error code is a unique identifier of the error type.
    /// </summary>
    public ErrorCode Code { get; }

    /// <summary>
    /// Gets the error message.
    /// Error message is a human-readable description of the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception if it caused the error.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the details of the error.
    /// </summary>
    public IReadOnlyCollection<ErrorDetail> Details { get; }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Message;

        foreach (var detail in Details)
        {
            yield return detail;
        }

        yield return Exception;
    }
}
