using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// A specification that is never satisfied, regardless of the context.
/// Useful as a default or placeholder. Also known as a contradiction.
/// </summary>
/// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
/// <param name="error">The error to return when evaluated.</param>
public class ContradictionSpecification<TContext>(Error error) : ISpecification<TContext>
{
    private readonly SpecificationResult _result = SpecificationResult.NotSatisfied(error);

    /// <inheritdoc />
    public SpecificationResult IsSatisfiedBy(TContext context)
        => _result;
}
