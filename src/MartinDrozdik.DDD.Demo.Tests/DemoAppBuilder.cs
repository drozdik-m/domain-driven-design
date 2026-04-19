using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppBuilder : TestedAppBuilder<Program>
{
    public DemoAppBuilder(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");
        WithOption<DatabaseOptions>(e => e.ConnectionString, $"Data Source={dbPath}");
        WithDisposable(() =>
        {
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
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
