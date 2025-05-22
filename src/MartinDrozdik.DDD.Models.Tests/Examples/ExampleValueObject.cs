using CSharpFunctionalExtensions;
using FluentValidation;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Templates.Errors;

namespace MartinDrozdik.DDD.Models.Tests.Examples;

/// <summary>
/// Example of a <see cref="Templates.ValueObject"/>.
/// </summary>
internal class ExampleValueObject
        : Templates.ValueObject
{
    private static readonly Validator s_validator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleValueObject"/> class.
    /// There are several reasons for the private constructor:
    /// <list type="bullet">
    ///     <item>Enables the properties to be purely readonly.</item>
    ///     <item>Makes it able to be easily mapped by EF Core.</item>
    /// </list>
    /// </summary>
    private ExampleValueObject(int value1, string value2)
    {
        s_validator.ValidateAndThrow((value1, value2));
        Value1 = value1;
        Value2 = value2;
    }

    /// <summary>
    /// Gets a random value #1.
    /// </summary>
    public int Value1 { get; }

    /// <summary>
    /// Gets a random value #2.
    /// </summary>
    public string Value2 { get; }

    /// <summary>
    /// Creates a new <b>Valid</b> instance of the <see cref="ExampleValueObject"/> class.
    /// </summary>
    /// <param name="value1">Random value #1.</param>
    /// <param name="value2">Random value #2.</param>
    /// <returns><b>Valid</b> instance of <see cref="ExampleValueObject"/> or an <see cref="Error"/>.</returns>
    public static Result<ExampleValueObject, Error> Create(int value1, string value2)
    {
        // Validate
        // (use tuple or an internal object)
        if (s_validator.Validate((value1, value2)).TryGetError(out var error))
        {
            return error;
        }

        // Return result
        var result = new ExampleValueObject(value1, value2);
        return Result.Success<ExampleValueObject, Error>(result);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value1;
        yield return Value2;
    }

    /// <summary>
    /// State validator for <see cref="ExampleValueObject"/>.
    /// </summary>
    public class Validator : AbstractValidator<(int Value1, string Value2)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        public Validator()
        {
            RuleFor(x => x.Value1)
                .NotEmpty()
                .GreaterThan(0);
            RuleFor(x => x.Value2)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
