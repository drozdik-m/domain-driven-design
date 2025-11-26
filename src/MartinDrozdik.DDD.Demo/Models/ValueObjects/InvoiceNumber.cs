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
        Validator.Instance.ValidateAndThrow((year, order));
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
    /// Creates a new valid instance of the <see cref="InvoiceNumber"/> class.
    /// </summary>
    public static Result<InvoiceNumber, Error> Create(int year, int order)
    {
        if (Validator.Instance.Validate((year, order)).TryGetError(out var error))
        {
            return error;
        }

        return new InvoiceNumber(year, order);
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
    private sealed class Validator : AbstractValidator<(int Year, int Order)>
    {
        public Validator()
        {
            RuleFor(x => x.Year).GreaterThan(2000);
            RuleFor(x => x.Order).GreaterThan(0);
        }

        public static Validator Instance { get; } = new();
    }
}
