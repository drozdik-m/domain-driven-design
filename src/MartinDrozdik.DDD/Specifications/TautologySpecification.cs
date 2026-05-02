namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// A specification that is always satisfied, regardless of the context.
/// Useful as a default or placeholder. Also known as a tautology.
/// </summary>
/// <typeparam name="TContext">The parameter to evaluate against the specification.</typeparam>
public class TautologySpecification<TContext> : ISpecification<TContext>
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="TautologySpecification{TContext}"/> class.
    /// </summary>
    public static TautologySpecification<TContext> Instance { get; } = new TautologySpecification<TContext>();

    /// <inheritdoc />
    public SpecificationResult IsSatisfiedBy(TContext context)
        => SpecificationResult.Satisfied;
}
