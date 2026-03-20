using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestProgramFactoryBuilder : TestWebApplicationFactoryBuilder<TestProgram>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");

    public TestProgramFactoryBuilder(ITestOutputHelper testOutputHelper)
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
