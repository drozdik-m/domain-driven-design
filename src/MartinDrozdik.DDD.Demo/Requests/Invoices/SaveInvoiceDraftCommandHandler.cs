using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public class SaveInvoiceDraftCommandHandler(InvoiceDbContext context) : ICommandHandler<SaveInvoiceDraftCommand, InvoiceId>
{
    public async Task<Result<InvoiceId, Error>> HandleAsync(SaveInvoiceDraftCommand command, CancellationToken cancellationToken)
    {
        // Handle it in a transaction
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Get persons
        Person? issuer = null;
        if (command.Data.Issuer is not null)
        {
            var issuerResult = await GetPerson(command.Data.Issuer, cancellationToken);
            if (!issuerResult.TryGetValue(out issuer))
            {
                return Result.Failure<InvoiceId, Error>(issuerResult.Error);
            }
        }

        var recipientResult = await GetPerson(command.Data.Recipient, cancellationToken);
        if (!recipientResult.TryGetValue(out var recipient))
        {
            return Result.Failure<InvoiceId, Error>(recipientResult.Error);
        }

        // The invoice already exists
        var existingInvoice = await context.Invoices.FindAsync(command.Data.Id, cancellationToken);
        if (existingInvoice is not null)
        {
            return new ErrorBuilder()
                .WithCode("Invoice.AlreadyExists")
                .WithMessage($"Invoice with ID '{command.Data.Id}' already exists.")
                .Build();
        }

        // Get invoice number
        var now = TimeProvider.System.GetLocalNow();
        var maxOrder = await context.Invoices
            .Where(i => i.Number.Year == now.Year)
            .MaxAsync(i => i.Number.Order, cancellationToken);
        var invoiceNumberResult = InvoiceNumber.Create(now.Year, maxOrder);
        if (!invoiceNumberResult.TryGetValue(out var invoiceNumber))
        {
            return Result.Failure<InvoiceId, Error>(invoiceNumberResult.Error);
        }

        // Get invoice
        var invoiceId = new InvoiceId(command.Data.Id);
        var invoiceResult = Invoice.CreateDraft(issuer, recipient, invoiceNumber);
        if (!invoiceResult.TryGetValue(out var invoice))
        {
            return Result.Failure<InvoiceId, Error>(invoiceResult.Error);
        }

        // Save invoice
        await context.Invoices.AddAsync(invoice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success<InvoiceId, Error>(invoice.Id);
    }

    private async Task<Result<Person, Error>> GetPerson(SaveInvoiceDraftCommand.Person person, CancellationToken cancellationToken)
    {
        var dbPerson = await context.People.SingleOrDefaultAsync(e => e.FullName == person.Name && e.DateOfBirth == person.DateOfBirth, cancellationToken);
        if (dbPerson is not null)
        {
            return Result.Success<Person, Error>(dbPerson);
        }

        var createResult = Person.Create(person.Name, person.DateOfBirth);
        return createResult;
    }
}
