using MartinDrozdik.DDD.Demo.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

public class InvoiceDbContextTests(ITestOutputHelper testOutputHelper) : DbContextIntegrationTests<InvoiceDbContext>
{
    protected override IDisposable GetContext(out InvoiceDbContext context)
    {
        var factory = new DemoAppFactory(testOutputHelper);
        var scope = factory.Services.CreateScope();
        context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        return scope;
    }
}
