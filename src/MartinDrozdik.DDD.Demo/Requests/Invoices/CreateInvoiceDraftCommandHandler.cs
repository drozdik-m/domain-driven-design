using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using MartinDrozdik.DDD.Models.Extensions;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public class CreateInvoiceDraftCommandHandler(InvoiceDbContext context) : ICommandHandler<CreateInvoiceDraftCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(CreateInvoiceDraftCommand command, CancellationToken cancellationToken)
    {
        // Get persons
        /*Person? issuer = null;
        if (command.Data.Issuer is not null)
        {
            issuer = await GetPerson(command.Data.Issuer, cancellationToken);
        }

        var recipient = await GetPerson(command.Data.Recipient, cancellationToken);

        // Get invoice number
        var now = TimeProvider.System.GetLocalNow();
        var maxOrder = await context.Invoices
            .Where(i => i.Number.Year == now.Year)
            .MaxAsync(i => i.Number.Order, cancellationToken);
        var invoiceNumber = InvoiceNumber.Create(now.Year, maxOrder);

        // Get invoice
        var invoiceId = new InvoiceId(Guid.CreateVersion7());
        var invoice = Invoice.CreateDraft(issuer, recipient, invoiceNumber);

        // Save invoice
        await context.Invoices.AddAsync(invoice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return invoice.Id;*/
        return new InvoiceId(Guid.NewGuid());
    }

    private async Task<Person> GetPerson(CreateInvoiceDraftCommand.Person person, CancellationToken cancellationToken)
    {
        var dbPerson = await context.People.SingleOrDefaultAsync(e => e.FullName == person.Name && e.DateOfBirth == person.DateOfBirth, cancellationToken);
        if (dbPerson is not null)
        {
            return dbPerson;
        }

        return Person.Create(person.Name, person.DateOfBirth);
    }
}
