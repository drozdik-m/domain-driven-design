using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactoryBuilder : TestWebApplicationFactoryBuilder<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");

    public DemoAppFactoryBuilder(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        WithOption<DatabaseOptions>(e => e.ConnectionString, $"Data Source={_dbPath}");
        WithDisposable(() =>
        {
            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch
                {
                    // Swallow exceptions, sqlite holds locks on the file and it may not be possible to delete immediately after tests run.
                    // The file will be cleaned up eventually by the OS temp file cleanup.
                }
            }
        });
    }
}
