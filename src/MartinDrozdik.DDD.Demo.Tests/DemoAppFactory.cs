using MartinDrozdik.DDD.Demo.Client.Generated;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _dbPath;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Action<IWebHostBuilder>? _config;

    public DemoAppFactory(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _dbPath = Path.Combine(_dbDir, "test_demo_app.db");
        testOutputHelper.WriteLine($"Test database path: {_dbPath}");

        if (!Directory.Exists(_dbDir))
        {
            Directory.CreateDirectory(_dbDir);
        }
        else
        {
            testOutputHelper.WriteLine($"!!! Test database directory already exists: {_dbDir}");
        }
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
                ["App:Database:ConnectionString"] = $"Data Source={_dbPath}",
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
                if (Directory.Exists(_dbDir))
                {
                    Directory.Delete(_dbDir, recursive: true);
                }
            }
            catch (Exception e)
            {
                var logger = Services.GetRequiredService<ILogger<DemoAppFactory>>();
                logger.LogError(e, "Failed to delete test database directory at {DbDir}", _dbDir);
                throw;
            }
        }

        base.Dispose(disposing);
    }
}
