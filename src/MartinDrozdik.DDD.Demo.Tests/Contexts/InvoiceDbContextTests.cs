using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Testing.Contexts;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

public class InvoiceDbContextTests(ITestOutputHelper testOutputHelper) : SqlDbContextIntegrationTests<InvoiceDbContext>
{
    private readonly DemoAppFactory _factory = new(testOutputHelper);

    protected override InvoiceDbContext GetContext()
    {
        return _factory.GetScopedService<InvoiceDbContext>();
    }
}
