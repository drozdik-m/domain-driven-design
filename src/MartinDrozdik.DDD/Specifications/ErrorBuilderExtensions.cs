using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Specifications;

/// <summary>
/// Extensions for <see cref="ISpecification{TContext}"/>.
/// </summary>
public static class ErrorBuilderExtensions
{
    /// <summary>
    /// Adds errors from <see cref="SpecificationResult"/> to the <see cref="ErrorBuilder"/> as sub-errors.
    /// </summary>
    /// <param name="errorBuilder">The builder to expand.</param>
    /// <param name="specificationResult">The result to extract errors from.</param>
    /// <returns>The updated <see cref="ErrorBuilder"/>.</returns>
    public static ErrorBuilder WithSpecificationResult(this ErrorBuilder errorBuilder, SpecificationResult specificationResult)
    {
        if (specificationResult.IsSatisfied)
        {
            throw new InvalidOperationException($"Cannot add errors from a satisfied specification result.");
        }

        return errorBuilder.WithSubErrors(specificationResult.Errors);
    }
}
