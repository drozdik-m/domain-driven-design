using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.FilePathProviders.Static;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestedWebAppBuilder : TestedAppBuilder<Program>
{
    public TestedWebAppBuilder(ITestOutputHelper testOutputHelper)
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
        WithOption<StaticFileVersioningOptions>(e => e.Version, "3.2.1");
    }
}
