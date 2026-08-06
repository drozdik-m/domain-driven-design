using FluentValidation;
using MartinDrozdik.DDD.Enumerations.Errors;

namespace MartinDrozdik.DDD.Enumerations.Validations;

/// <summary>
/// Validation rules mapping a plain .NET <see cref="Enum"/> onto an <see cref="Enumeration"/>.
/// </summary>
public static class EnumerationRuleBuilderExtensions
{
    /// <summary>
    /// Validates that the .NET enum member has a matching <typeparamref name="TEnumeration"/> member.
    /// </summary>
    /// <remarks>
    /// Model binding happily produces values that are not defined members of their enum type, for example from <c>?state=99</c>.
    /// Such a value maps to nothing and would otherwise only fail later, when it is converted.
    /// This rule turns it into an ordinary validation failure.
    /// </remarks>
    /// <example>
    /// <code>
    /// RuleFor(x => x.State).MustMapToEnumeration(EnumerationMap.To&lt;InvoiceState&gt;());
    /// </code>
    /// </example>
    /// <typeparam name="T">Type of the validated object.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <param name="ruleBuilder">The rule builder this method is extending.</param>
    /// <param name="mapper">The targeted enumeration, built by <see cref="EnumerationMap.To{TEnumeration}"/>.</param>
    /// <returns>The rule builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">When the rule builder is null.</exception>
    public static IRuleBuilderOptions<T, TEnum> MustMapToEnumeration<T, TEnum, TEnumeration>(
        this IRuleBuilder<T, TEnum> ruleBuilder,
        EnumerationMapper<TEnumeration> mapper)
        where TEnum : struct, Enum
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);

        return ruleBuilder
            .Must(value => mapper.CanMap(value))
            .WithErrorCode(EnumerationErrorCodes.EnumerationNameNotFound.Key)
            .WithMessage($"'{{PropertyValue}}' is not a valid {typeof(TEnumeration).Name}.");
    }

    /// <summary>
    /// Validates that the optional .NET enum member has a matching <typeparamref name="TEnumeration"/> member.
    /// </summary>
    /// <remarks>
    /// A null value passes.
    /// Combine with <see cref="DefaultValidatorExtensions.NotNull{T, TProperty}"/> when the value is required.
    /// </remarks>
    /// <typeparam name="T">Type of the validated object.</typeparam>
    /// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
    /// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
    /// <param name="ruleBuilder">The rule builder this method is extending.</param>
    /// <param name="mapper">The targeted enumeration, built by <see cref="EnumerationMap.To{TEnumeration}"/>.</param>
    /// <returns>The rule builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">When the rule builder is null.</exception>
    public static IRuleBuilderOptions<T, TEnum?> MustMapToEnumeration<T, TEnum, TEnumeration>(
        this IRuleBuilder<T, TEnum?> ruleBuilder,
        EnumerationMapper<TEnumeration> mapper)
        where TEnum : struct, Enum
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);

        return ruleBuilder
            .Must(value => value is null || mapper.CanMap(value.Value))
            .WithErrorCode(EnumerationErrorCodes.EnumerationNameNotFound.Key)
            .WithMessage($"'{{PropertyValue}}' is not a valid {typeof(TEnumeration).Name}.");
    }
}
