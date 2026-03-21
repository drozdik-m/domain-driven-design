using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Contexts;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

public class InvoiceDbContextTests(ITestOutputHelper testOutputHelper) : SqlDbContextIntegrationTests<InvoiceDbContext>, IDisposable
{
    private readonly TestedApp<Program> _factory =
        new DemoAppBuilder(testOutputHelper).Build();

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override InvoiceDbContext GetContext()
    {
        return _factory.GetScopedService<InvoiceDbContext>();
    }
}
