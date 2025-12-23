using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

public class GetInvoicesQueryHandler(InvoiceDbContext context) : IQueryHandler<GetInvoicesQuery, GetInvoicesQuery.Response>
{
    public async Task<Result<GetInvoicesQuery.Response, Error>> HandleAsync(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var invoices = await context.Invoices
            .Select(i => new GetInvoicesQuery.Item
            {
                Id = i.Id.Key,
                IssuerName = i.Issuer != null ? i.Issuer.FullName : null,
                RecipientName = i.Recipient.FullName,
                InvoiceNumber = i.Number.ToString(),
                State = i.State.ToString()
            })
            .ToListAsync(cancellationToken);

        var response = new GetInvoicesQuery.Response
        {
            Items = invoices
        };

        return Result.Success<GetInvoicesQuery.Response, Error>(response);
    }
}
