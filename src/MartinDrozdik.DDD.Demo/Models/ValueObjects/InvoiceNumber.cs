using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Demo.Models.ValueObjects;

/// <summary>
/// Represents a business identifier for an invoice.
/// For example, "2023/15" where 2023 is the year and 15 is the order number.
/// </summary>
public class InvoiceNumber : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceNumber"/> class.
    /// </summary>
    private InvoiceNumber(int year, int order)
    {
        Year = year;
        Order = order;
    }

    /// <summary>
    /// Gets the year of the invoice.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// Gets the order number of the invoice.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets an empty or default invoice number instance.
    /// </summary>
    /// <remarks>
    /// Use this property to represent an uninitialized or placeholder invoice number when no valid value is available.
    /// </remarks>
    public static InvoiceNumber Empty { get; } = new(2000, 1);

    /// <summary>
    /// Creates a new valid instance of the <see cref="InvoiceNumber"/> class.
    /// </summary>
    public static InvoiceNumber Create(int year, int order)
    {
        var result = new InvoiceNumber(year, order);
        Validator.Instance.ValidateAndThrowBusiness(result);
        return result;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Year}/{Order}";
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Order;
    }

    /// <summary>
    /// State validator for <see cref="InvoiceNumber"/>.
    /// </summary>
    private sealed class Validator : AbstractValidator<InvoiceNumber>
    {
        public Validator()
        {
            RuleFor(x => x.Year).GreaterThan(2000);
            RuleFor(x => x.Order).GreaterThan(0);
        }

        public static Validator Instance { get; } = new();
    }
}
