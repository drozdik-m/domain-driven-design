using MartinDrozdik.DDD.Results;

namespace MartinDrozdik.DDD.Errors;

/// <summary>
/// Builder for creating instances of <see cref="Error"/>.
/// </summary>
public class ErrorBuilder
{
    private readonly List<ErrorDetail> _details = [];
    private ErrorCode? _code;
    private string? _message;
    private Exception? _exception;

    /// <summary>
    /// Sets the error code.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithCode(ErrorCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        _code = code;
        return this;
    }

    /// <summary>
    /// Sets the error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithMessage(string message)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(message);
        _message = message;
        return this;
    }

    /// <summary>
    /// Adds an error detail.
    /// </summary>
    /// <param name="detail">The error detail.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithDetail(ErrorDetail detail)
    {
        _details.Add(detail);
        return this;
    }

    /// <summary>
    /// Adds an error detail.
    /// </summary>
    /// <param name="key">The error detail key.</param>
    /// <param name="value">The error detail value.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithDetail(string key, string value)
    {
        return WithDetail(new ErrorDetail(key, value));
    }

    /// <summary>
    /// Adds error details.
    /// </summary>
    /// <param name="details">The details to add.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithDetails(params IEnumerable<ErrorDetail> details)
    {
        ArgumentNullException.ThrowIfNull(details);
        _details.AddRange(details);
        return this;
    }

    /// <summary>
    /// Adds error details created from <see cref="Errors"/>s.
    /// Flattens the sub-errors by their code, value and details. Ignores exceptions.
    /// </summary>
    /// <param name="subErrors">The sub-errors to add.</param>
    /// <returns>The current instance of <see cref="Error"/>.</returns>
    public ErrorBuilder WithSubErrors(params IEnumerable<Error> subErrors)
    {
        ArgumentNullException.ThrowIfNull(subErrors);
        foreach (var subError in subErrors)
        {
            WithDetail(subError.Code.Key, subError.Message);
            WithDetails(subError.Details);
        }

        return this;
    }

    /// <summary>
    /// Sets the exception that caused the error.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The current instance of <see cref="ErrorBuilder"/>.</returns>
    public ErrorBuilder WithCause(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exception = exception;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Error"/> instance.
    /// </summary>
    /// <returns>A new instance of <see cref="Error"/>.</returns>
    public Error Build()
    {
        ArgumentNullException.ThrowIfNull(_code);
        ArgumentNullException.ThrowIfNull(_message);
        return new Error(_code, _message, _details, _exception);
    }

    /// <summary>
    /// Builds a <see cref="UnitResult{TError}"/> representing a failure with the built <see cref="Error"/>.
    /// </summary>
    /// <returns>A new instance of <see cref="UnitResult{Error}"/>.</returns>
    public UnitResult<Error> BuildUnitResult()
    {
        var error = Build();
        return UnitResult.Failure(error);
    }
}
