using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public record CreateInvoiceDraftCommand(CreateInvoiceDraftCommand.Request Data) : ICommand<InvoiceId>
{
    public class Request
    {
        //public required Person? Issuer { get; init; }
        //public required Person Recipient { get; init; }
    }

    public class Person
    {
        public required string Name { get; init; }
        public required DateTimeOffset DateOfBirth { get; init; }
    }
}
