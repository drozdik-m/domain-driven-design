using MartinDrozdik.DDD.Web.Options;

namespace MartinDrozdik.DDD.Demo.Options;

public class InvoiceOptions : IValidatedAppOptions<InvoiceOptions>
{
    /// <inheritdoc />
    public static string Section { get; } = "Invoice";

    /// <inheritdoc />
    public static AbstractValidator<InvoiceOptions> Validator { get; } = new OptionsValidator();

    public required int StartingId { get; init; }

    public required string DefaultName { get; init; }

    private class OptionsValidator : AbstractValidator<InvoiceOptions>
    {
        public OptionsValidator()
        {
            RuleFor(e => e.StartingId).GreaterThanOrEqualTo(0);
            RuleFor(e => e.DefaultName).NotNull().NotEmpty();
        }
    }
}
