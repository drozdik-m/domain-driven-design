using MartinDrozdik.DDD.Demo.Models.Enumerations;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;

namespace MartinDrozdik.DDD.Demo.Models.Aggregates;

/// <summary>
/// Represents an invoice issued by an issuer to a recipient.
/// Each invoice has a unique invoice number.
/// </summary>
public class Invoice : IAggregateRoot<InvoiceId>
{
    private Invoice()
    {
    }

    public InvoiceId Id { get; private set; } = new InvoiceId(Guid.NewGuid());

    //public PersonId? IssuerId { get; private set; }
    //public Person? Issuer { get; private set; }

    //public PersonId RecipientId { get; private set; }
    //public Person Recipient { get; private set; }

    public InvoiceNumber Number { get; private set; } = InvoiceNumber.Empty;

    public InvoiceState State { get; private set; } = InvoiceState.Draft;

    /// <summary>
    /// Creates a new valid instance of the <see cref="Invoice"/> class.
    /// </summary>
    /// <returns>New instance of <see cref="Invoice"/> or an <see cref="Error"/>.</returns>
    /*public static Result<Invoice, Error> CreateDraft(Person issuer, Person recipient, InvoiceNumber number)
    {
        var id = new InvoiceId(Guid.CreateVersion7());
        return new Invoice(id, issuer, recipient, number, InvoiceState.Draft);
    }

    /// <summary>
    /// Issues the invoice, changing its state from Draft to Issued.
    /// </summary>
    public UnitResult<Error> IssueTo(Person issuerId)
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
    }*/
}
