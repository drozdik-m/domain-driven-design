using MartinDrozdik.DDD.Mediator.Queries;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public record GetInvoicesQuery : IQuery<GetInvoicesQuery.Response>
{
    public class Response
    {
        public required List<Item> Items { get; init; }
    }

    public class Item
    {
        public required Guid Id { get; init; }
        public required string? IssuerName { get; init; }
        public required string RecipientName { get; init; }
        public required string InvoiceNumber { get; init; }
        public required InvoiceStateApi State { get; init; }
    }
}
