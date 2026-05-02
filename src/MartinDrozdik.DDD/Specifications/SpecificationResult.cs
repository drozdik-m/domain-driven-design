using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Represents the result of evaluating a specification, indicating whether the specification is satisfied and providing any associated errors.
/// </summary>
/// <remarks>
/// Is boolean-like: it can be implicitly converted to a boolean indicating whether the specification is satisfied, but also contains an optional list of errors explaining why it was not satisfied.
/// </remarks>
public readonly struct SpecificationResult
{
    private readonly IReadOnlyList<Error>? _errors;

    private SpecificationResult(IReadOnlyList<Error>? errors)
    {
        _errors = errors;

        if (_errors?.Count == 0)
        {
            _errors = null; // Normalize empty errors to null for consistency
        }
    }

    /// <summary>
    /// Gets a satisfied result.
    /// </summary>
    public static SpecificationResult Satisfied { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the specification is satisfied.
    /// </summary>
    public bool IsSatisfied => _errors is null;

    /// <summary>
    /// Gets errors explaining why the specification was not satisfied.
    /// Empty when satisfied.
    /// </summary>
    public IReadOnlyList<Error> Errors => _errors ?? [];

    /// <summary>
    /// Implicit bool conversion for easy use in conditions.
    /// </summary>
    /// <param name="result">The specification result to convert.</param>
    public static implicit operator bool(SpecificationResult result)
    {
        return result.IsSatisfied;
    }

    /// <summary>
    /// Implicit conversion from an error to a not-satisfied result, allowing easy creation of failure results from errors.
    /// </summary>
    /// <param name="error">The error explaining why the specification was not satisfied.</param>
    public static implicit operator SpecificationResult(Error error)
    {
        return NotSatisfied(error);
    }

    /// <inheritdoc cref="And(SpecificationResult, SpecificationResult)"/>
    public static SpecificationResult operator &(SpecificationResult left, SpecificationResult right) => And(left, right);

    /// <inheritdoc cref="Or(SpecificationResult, SpecificationResult)"/>
    public static SpecificationResult operator |(SpecificationResult left, SpecificationResult right) => Or(left, right);

    /// <summary>
    /// Defines the true operator to allow using <see cref="SpecificationResult"/> in boolean contexts (e.g., if statements).
    /// </summary>
    /// <param name="result">The specification result to evaluate.</param>
    /// <returns>True if the specification is satisfied; otherwise, false.</returns>
    public static bool operator true(SpecificationResult result)
        => result.IsSatisfied;

    /// <summary>
    /// Defines the false operator to allow using <see cref="SpecificationResult"/> in boolean contexts (e.g., if statements).
    /// </summary>
    /// <param name="result">The specification result to evaluate.</param>
    /// <returns>False if the specification is satisfied; otherwise, true.</returns>
    public static bool operator false(SpecificationResult result)
        => !result.IsSatisfied;

    /// <summary>
    /// Gets a not-satisfied result with failures.
    /// </summary>
    /// <param name="errors">The errors explaining why the specification was not satisfied.</param>
    /// <returns>A new not-satisfied result containing the provided errors.</returns>
    public static SpecificationResult NotSatisfied(IEnumerable<Error> errors)
    {
        var errorList = errors.ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        return new(errorList);
    }

    /// <summary>
    /// Gets a not-satisfied result with failures.
    /// </summary>
    /// <param name="error">The error explaining why the specification was not satisfied.</param>
    /// <returns>A new not-satisfied result containing the provided error.</returns>
    public static SpecificationResult NotSatisfied(Error error)
    {
        return new([error]);
    }

    /// <summary>
    /// Merges two results with AND logic: both must be satisfied, errors are aggregated.
    /// </summary>
    /// <param name="left">The left specification result.</param>
    /// <param name="right">The right specification result.</param>
    /// <returns>A new specification result representing the AND combination of the two results.</returns>
    public static SpecificationResult And(
        SpecificationResult left,
        SpecificationResult right)
    {
        if (left.IsSatisfied && right.IsSatisfied)
        {
            return Satisfied;
        }

        if (left.IsSatisfied)
        {
            return right;
        }
        else if (right.IsSatisfied)
        {
            return left;
        }

        var errors = left.Errors.Concat(right.Errors).ToArray();
        return new(errors);
    }

    /// <summary>
    /// Merges two results with OR logic:
    /// either must be satisfied, errors reported only if both fail.
    /// </summary>
    /// <param name="left">The left specification result.</param>
    /// <param name="right">The right specification result.</param>
    /// <returns>A new specification result representing the OR combination of the two results.</returns>
    public static SpecificationResult Or(
        SpecificationResult left,
        SpecificationResult right)
    {
        if (left.IsSatisfied || right.IsSatisfied)
        {
            return Satisfied;
        }

        var errors = left.Errors.Concat(right.Errors).ToArray();
        return new(errors);
    }
}
