using MartinDrozdik.DDD.Demo.Client.Generated;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Action<IWebHostBuilder>? _config;

    public DemoAppFactory(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        testOutputHelper.WriteLine($"Test database path: {_dbPath}");
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
            // Add your test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddXUnit(_testOutputHelper);
        });

        _config?.Invoke(builder);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && File.Exists(_dbPath))
        {
            // Clean up the test database file
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        base.Dispose(disposing);
    }
}
