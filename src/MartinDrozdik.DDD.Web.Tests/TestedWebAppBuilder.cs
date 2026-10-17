using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.FilePathProviders.Static;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestedWebAppBuilder : TestedAppBuilder<Program>
{
    public TestedWebAppBuilder(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");
        ConnectionString = $"Data Source={dbPath}";
        WithOption<DatabaseOptions>(e => e.ConnectionString, ConnectionString);
        WithDisposable(() =>
        {
            DeleteDatabase(dbPath);
            DeleteDatabase(SecondTestDbContext.GetDatabasePath(dbPath));
        });
        WithOption<StaticFileVersioningOptions>(e => e.Version, "3.2.1");
    }

    /// <summary>
    /// Gets the connection string of the database of this app, e.g. to point a second app at the same database.
    /// </summary>
    public string ConnectionString { get; }

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Swallow exceptions, sqlite holds locks on the file and it may not be possible to delete immediately after tests run.
                // The file will be cleaned up eventually by the OS temp file cleanup.
            }
        }
    }
}
