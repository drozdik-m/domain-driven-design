using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Extensions for <see cref="ISpecification{TContext}"/>.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Evaluates whether the given context satisfies the criteria defined by the specification.
    /// </summary>
    /// <typeparam name="TContext"> The parameter to evaluate against the specification.</typeparam>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="context">The context to evaluate against the specification.</param>
    /// <param name="result">An output parameter that will contain the result of the evaluation.</param>
    /// <returns>Boolean indicating whether the context satisfies the specification.</returns>
    public static bool TrySatisfyBy<TContext>(this ISpecification<TContext> specification, TContext context, out SpecificationResult result)
    {
        result = specification.IsSatisfiedBy(context);
        return result;
    }

    /// <summary>
    /// Combines specification using the AND logical operator.
    /// </summary>
    /// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
    /// <param name="specification">One of the joined specifications.</param>
    /// <param name="specifications">All other joined specifications.</param>
    /// <returns>New specification that combined parameters using the AND logical operator.</returns>
    public static ISpecification<TContext> And<TContext>(this ISpecification<TContext> specification, params ISpecification<TContext>[] specifications)
        => new AndSpecification<TContext>(specification, specifications);

    /// <summary>
    /// Combines specification using the OR logical operator.
    /// </summary>
    /// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
    /// <param name="specification">One of the joined specifications.</param>
    /// <param name="specifications">All other joined specifications.</param>
    /// <returns>New specification that combined parameters using the OR logical operator.</returns>
    public static ISpecification<TContext> Or<TContext>(this ISpecification<TContext> specification, params ISpecification<TContext>[] specifications)
        => new OrSpecification<TContext>(specification, specifications);

    /// <summary>
    /// Negates the specification, indicating that the specification is satisfied when the inner specification is not satisfied, and vice versa.
    /// </summary>
    /// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
    /// <param name="specification">The specification to negate.</param>
    /// <param name="error">The error to associate with the negated specification.</param>
    /// <returns>A new specification that represents the negation of the original specification.</returns>
    public static ISpecification<TContext> Not<TContext>(this ISpecification<TContext> specification, Error error)
        => new NotSpecification<TContext>(specification, error);
}
