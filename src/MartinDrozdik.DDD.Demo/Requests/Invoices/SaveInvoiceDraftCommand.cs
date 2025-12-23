using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public record SaveInvoiceDraftCommand(SaveInvoiceDraftCommand.Request Data) : ICommand<InvoiceId>
{
    public class Request
    {
        public required Guid Id { get; init; }
        public required Person? Issuer { get; init; }
        public required Person Recipient { get; init; }
    }

    public class Person
    {
        public required string Name { get; init; }
        public required DateTime DateOfBirth { get; init; }
    }
}
