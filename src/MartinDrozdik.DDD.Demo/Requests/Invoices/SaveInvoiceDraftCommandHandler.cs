using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using MartinDrozdik.DDD.Models.Extensions;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public class SaveInvoiceDraftCommandHandler(InvoiceDbContext context) : ICommandHandler<SaveInvoiceDraftCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(SaveInvoiceDraftCommand command, CancellationToken cancellationToken)
    {
        // Handle it in a transaction
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Get persons
        Person? issuer = null;
        if (command.Data.Issuer is not null)
        {
            issuer = await GetPerson(command.Data.Issuer, cancellationToken);
        }

        var recipient = await GetPerson(command.Data.Recipient, cancellationToken);

        // The invoice already exists
        var existingInvoice = await context.Invoices.FindAsync(command.Data.Id, cancellationToken);
        if (existingInvoice is not null)
        {
            throw new ErrorBuilder()
                .WithCode("Invoice.AlreadyExists")
                .WithMessage($"Invoice with ID '{command.Data.Id}' already exists.")
                .BuildBusinessException();
        }

        // Get invoice number
        var now = TimeProvider.System.GetLocalNow();
        var maxOrder = await context.Invoices
            .Where(i => i.Number.Year == now.Year)
            .MaxAsync(i => i.Number.Order, cancellationToken);
        var invoiceNumber = InvoiceNumber.Create(now.Year, maxOrder);

        // Get invoice
        var invoiceId = new InvoiceId(command.Data.Id);
        var invoice = Invoice.CreateDraft(issuer, recipient, invoiceNumber);

        // Save invoice
        await context.Invoices.AddAsync(invoice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }

    private async Task<Person> GetPerson(SaveInvoiceDraftCommand.Person person, CancellationToken cancellationToken)
    {
        var dbPerson = await context.People.SingleOrDefaultAsync(e => e.FullName == person.Name && e.DateOfBirth == person.DateOfBirth, cancellationToken);
        if (dbPerson is not null)
        {
            return dbPerson;
        }

        return Person.Create(person.Name, person.DateOfBirth);
    }
}
