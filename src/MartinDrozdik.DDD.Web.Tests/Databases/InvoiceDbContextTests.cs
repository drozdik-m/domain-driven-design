using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Contexts;
using Xunit.Abstractions;
using static MartinDrozdik.DDD.Web.Tests.TestProgram;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

public class TestDbContextTests(ITestOutputHelper testOutputHelper) : SqlDbContextIntegrationTests<TestDbContext>, IDisposable
{
    private readonly TestWebApplicationFactory<TestProgram> _factory
        = new TestProgramFactoryBuilder(testOutputHelper).Build();

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
