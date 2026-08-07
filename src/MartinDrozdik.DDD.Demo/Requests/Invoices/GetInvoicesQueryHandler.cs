using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Mediator.Queries;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public class GetInvoicesQueryHandler(InvoiceDbContext context) : IQueryHandler<GetInvoicesQuery, GetInvoicesQuery.Response>
{
    public async Task<GetInvoicesQuery.Response> HandleAsync(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var rows = await context.Invoices.ToListAsync(cancellationToken);
        var invoices = rows
            .Select(r => new GetInvoicesQuery.Item
            {
                Id = r.Id.Key,
                IssuerName = r.Issuer?.FullName,
                RecipientName = r.Recipient.FullName,
                InvoiceNumber = r.Number.ToString(),
                State = r.State.ToStructEnum<InvoiceStateApi>()
            })
            .ToList();

        return new GetInvoicesQuery.Response
        {
            Items = invoices,
        };
    }
}
