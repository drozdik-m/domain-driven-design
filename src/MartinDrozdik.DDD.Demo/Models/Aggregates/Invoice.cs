using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.Enumerations;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Templates;

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

    public PersonId? IssuerId { get; private set; }
    public Person? Issuer { get; private set; }

    public PersonId RecipientId { get; init; } = null!;
    public Person Recipient { get; init; } = null!;

    public InvoiceNumber Number { get; private set; } = InvoiceNumber.Empty;

    public InvoiceState State { get; private set; } = InvoiceState.Draft;

    /// <summary>
    /// Creates a new valid instance of the <see cref="Invoice"/> class.
    /// </summary>
    /// <returns>New instance of <see cref="Invoice"/> or an <see cref="Error"/>.</returns>
    public static Invoice CreateDraft(Person? issuer, Person recipient, InvoiceNumber number)
    {
        var id = new InvoiceId(Guid.CreateVersion7());
        return new Invoice()
        {
            Id = id,
            IssuerId = issuer?.Id,
            Issuer = issuer,
            RecipientId = recipient.Id,
            Recipient = recipient,
            Number = number,
            State = InvoiceState.Draft,
        };
    }

    /*public void ChangeIssuer(Person newIssuer)
    {
        var spec = new IsDraftSpecification().And(new HasNoIssuerSpecification());
        if (!spec.TrySatisfyBy(this, out var specResult))
        {
            throw new ErrorBuilder()
                .WithCode("CannotChangeIssuer")
                .WithMessage("The issuer of the invoice cannot be changed.")
                .WithSpecificationResult(specResult)
                .BuildValidationException();
        }

        Issuer = newIssuer;
        IssuerId = newIssuer.Id;
    }*/

    /// <summary>
    /// Issues the invoice, changing its state from Draft to Issued.
    /// </summary>
    public void IssueTo(Person issuerId)
    {
        if (State != InvoiceState.Draft)
        {
            throw new ErrorBuilder()
                .WithCode("OnlyDraftsCanBeIssued")
                .WithMessage($"Only draft invoices can be issued. This invoice is in the {State} state.")
                .BuildValidationException();
        }

        Issuer = issuerId;
        State = InvoiceState.Issued;
    }

    /*private class IsDraftSpecification : ISpecification<Invoice>
    {
        public SpecificationResult IsSatisfiedBy(Invoice invoice)
        {
            if (invoice.State != InvoiceState.Draft)
            {
                return new ErrorBuilder()
                    .WithCode("InvoiceMustBeDraft")
                    .WithMessage($"The invoice must be in the {InvoiceState.Draft} state.")
                    .Build();
            }

            return SpecificationResult.Satisfied;
        }
    }

    private class HasNoIssuerSpecification : ISpecification<Invoice>
    {
        public SpecificationResult IsSatisfiedBy(Invoice invoice)
        {
            if (invoice.IssuerId != null)
            {
                return new ErrorBuilder()
                    .WithCode("InvoiceMustHaveNoIssuer")
                    .WithMessage("The invoice must not have an issuer.")
                    .Build();
            }

            return SpecificationResult.Satisfied;
        }
    }*/
}
