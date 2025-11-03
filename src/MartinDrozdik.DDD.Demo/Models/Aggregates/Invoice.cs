using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.Enumerations;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;

namespace MartinDrozdik.DDD.Demo.Models.Aggregates;

/// <summary>
/// Represents an invoice issued by an issuer to a recipient.
/// Each invoice has a unique invoice number.
/// </summary>
public class Invoice : AggregateRoot<InvoiceId>
{
    public Invoice(InvoiceId id, PersonId? issuer, PersonId recipient, InvoiceNumber number, InvoiceState state)
        : base(id)
    {
        Issuer = issuer;
        Recipient = recipient;
        Number = number;
        State = state;
    }

    public PersonId? Issuer { get; private set; }

    public PersonId Recipient { get; }

    public InvoiceNumber Number { get; }

    public InvoiceState State { get; private set; }

    /// <summary>
    /// Creates a new valid instance of the <see cref="Invoice"/> class.
    /// </summary>
    /// <returns>New instance of <see cref="Invoice"/> or an <see cref="Error"/>.</returns>
    public static Result<Invoice, Error> CreateDraft(PersonId issuer, PersonId recipient, InvoiceNumber number)
    {
        var id = new InvoiceId(Guid.NewGuid());
        return new Invoice(id, issuer, recipient, number, InvoiceState.Draft);
    }

    /// <summary>
    /// Issues the invoice, changing its state from Draft to Issued.
    /// </summary>
    public UnitResult<Error> IssueTo(PersonId issuerId)
    {
        if (State != InvoiceState.Draft)
        {
            return new ErrorBuilder()
                .WithCode("OnlyDraftsCanBeIssued")
                .WithMessage($"Only draft invoices can be issued. This invoice is in the {State} state.")
                .BuildUnitResult();
        }

        Issuer = issuerId;
        State = InvoiceState.Issued;
        return UnitResult.Success<Error>();
    }
}
