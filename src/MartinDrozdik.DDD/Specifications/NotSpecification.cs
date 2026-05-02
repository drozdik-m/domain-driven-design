using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Negates a specification, indicating that the specification is satisfied when the inner specification is not satisfied, and vice versa.
/// </summary>
/// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
/// <param name="specification">The specification to negate.</param>
/// <param name="error">The error to return when the inner specification is satisfied, indicating that the negated specification is not satisfied.</param>
public class NotSpecification<TContext>(ISpecification<TContext> specification, Error error) : ISpecification<TContext>
{
    private readonly SpecificationResult _notSatisfiedResult = SpecificationResult.NotSatisfied(error);

    /// <inheritdoc />
    public SpecificationResult IsSatisfiedBy(TContext context)
    {
        var result = specification.IsSatisfiedBy(context);
        return result
            ? _notSatisfiedResult
            : SpecificationResult.Satisfied;
    }
}
