using MartinDrozdik.DDD.Demo.Client.Generated;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactory : WebApplicationFactory<Program>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Action<IWebHostBuilder>? _config;
    private readonly string _connectionString;
    private SqliteConnection? _keepAliveConnection;

    public DemoAppFactory(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        // Create a unique in-memory database per factory instance
        var dbName = Guid.NewGuid().ToString();
        _connectionString = $"Data Source={dbName};Mode=Memory";

        testOutputHelper.WriteLine($"Test in-memory database: {dbName}");
    }

    public DemoAppFactory(ITestOutputHelper testOutputHelper, Action<IWebHostBuilder> config)
        : this(testOutputHelper)
    {
        _config = config;
    }

    public DddClient CreateDddClient()
    {
        var httpClient = CreateClient();
        var authProvider = new AnonymousAuthenticationProvider();
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = httpClient.BaseAddress?.ToString()
                ?? throw new InvalidOperationException("HttpClient BaseAddress is null"),
        };
        var client = new DddClient(requestAdapter);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddXUnit(_testOutputHelper);
        });

        // Keep a connection alive to preserve the in-memory database
        builder.ConfigureServices(services =>
        {
            // Open and keep alive a connection to maintain the in-memory database
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();

            _testOutputHelper.WriteLine("In-memory database connection opened and kept alive");
        });

        _config?.Invoke(builder);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _keepAliveConnection?.Close();
                _keepAliveConnection?.Dispose();
                _testOutputHelper.WriteLine("In-memory database connection closed");
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine($"Error disposing in-memory connection: {e.Message}");
            }
        }

        base.Dispose(disposing);
    }
}
