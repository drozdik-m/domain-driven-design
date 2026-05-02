using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Combines multiple specifications using a logical OR operation.
/// </summary>
/// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
public class OrSpecification<TContext> : ISpecification<TContext>
{
    private readonly ISpecification<TContext>[] _specifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{TContext}"/> class.
    /// </summary>
    /// <param name="specifications">The specifications to combine using a logical OR operation.</param>
    public OrSpecification(params IEnumerable<ISpecification<TContext>> specifications)
    {
        _specifications = [.. specifications];
        ArgumentOutOfRangeException.ThrowIfZero(_specifications.Length);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{TContext}"/> class.
    /// </summary>
    /// <param name="specification">A specifications to combine using a logical OR operation.</param>
    /// <param name="others">The specifications to combine using a logical OR operation.</param>
    public OrSpecification(ISpecification<TContext> specification, params ISpecification<TContext>[] others)
    {
        _specifications = [specification, .. others];
        ArgumentOutOfRangeException.ThrowIfZero(_specifications.Length);
    }

    /// <inheritdoc />
    public SpecificationResult IsSatisfiedBy(TContext context)
    {
        ArgumentOutOfRangeException.ThrowIfZero(_specifications.Length);

        List<Error>? errors = null; // Postpone allocations
        foreach (var specification in _specifications)
        {
            var result = specification.IsSatisfiedBy(context);
            if (result)
            {
                return SpecificationResult.Satisfied;
            }

            errors ??= [];
            errors.AddRange(result.Errors);
        }

        return errors is null
            ? SpecificationResult.Satisfied
            : SpecificationResult.NotSatisfied(errors);
    }
}
