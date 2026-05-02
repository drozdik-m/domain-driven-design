namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Specification is a contract / validation rule.
/// Used to evaluate whether a given context satisfies certain criteria.
/// </summary>
/// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
public interface ISpecification<in TContext>
{
    /// <summary>
    /// Evaluates whether the given context satisfies the criteria defined by the specification.
    /// </summary>
    /// <param name="context">The context to evaluate against the specification.</param>
    /// <returns>An object indicating whether the context satisfies the specification. Implicitly converts to boolean and optionally provides errors.</returns>
    SpecificationResult IsSatisfiedBy(TContext context);
}
