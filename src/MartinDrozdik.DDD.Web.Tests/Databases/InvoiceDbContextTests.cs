using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Contexts;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

public class TestDbContextTests(ITestOutputHelper testOutputHelper) : SqlDbContextIntegrationTests<TestDbContext>, IDisposable
{
    private readonly TestedApp<Program> _factory
        = new TestedWebAppBuilder(testOutputHelper).Build();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }
    }

    protected override TestDbContext GetContext()
    {
        return _factory.GetScopedService<TestDbContext>();
    }
}
