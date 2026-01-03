using MartinDrozdik.DDD.Demo.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

public class InvoiceDbContextTests(ITestOutputHelper testOutputHelper) : DbContextIntegrationTests<InvoiceDbContext>
{
    private readonly DemoAppFactory _factory = new(testOutputHelper);

    protected override IDisposable GetContext(out InvoiceDbContext context)
    {
        var scope = _factory.Services.CreateScope();
        context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        return scope;
    }
}
