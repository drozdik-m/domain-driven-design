using System.Diagnostics;

namespace MartinDrozdik.DDD.Models.Errors;

/// <summary>
/// Represents an aggregate error that contains multiple errors.
/// </summary>
[DebuggerDisplay("{Errors.Count} errors")]
public class AggregateError : Error
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateError"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="errors">The collection of errors.</param>
    public AggregateError(ErrorCode code, string message, IEnumerable<Error> errors)
        : base(code, message, [], null)
    {
        Errors = errors.ToArray();
    }

    /// <summary>
    /// Gets the collection of errors.
    /// </summary>
    public IReadOnlyCollection<Error> Errors { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="AggregateError"/> class with the specified errors.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="errors">The collection of errors.</param>
    /// <returns>A new instance of the <see cref="AggregateError"/> class.</returns>
    public static AggregateError Create(ErrorCode code, string message, params Error[] errors)
    {
        return new AggregateError(code, message, errors);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var component in base.GetEqualityComponents())
        {
            yield return component;
        }

        foreach (var error in Errors)
        {
            yield return error;
        }
    }
}
