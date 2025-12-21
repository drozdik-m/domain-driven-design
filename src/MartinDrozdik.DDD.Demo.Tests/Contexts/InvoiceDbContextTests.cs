using MartinDrozdik.DDD.Demo.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

public class InvoiceDbContextTests(DemoAppFactory factory)
        : DbContextIntegrationTests<InvoiceDbContext>, IClassFixture<DemoAppFactory>
{
    protected override IDisposable GetContext(out InvoiceDbContext context)
    {
        var scope = factory.Services.CreateScope();
        context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        return scope;
    }
}
