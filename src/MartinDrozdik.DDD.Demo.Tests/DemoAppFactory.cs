using MartinDrozdik.DDD.Demo.Client.Generated;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactory(ITestOutputHelper testOutputHelper) : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");
    private readonly Action<IWebHostBuilder>? _config;

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
        builder.SetOption<DatabaseOptions>(e => e.ConnectionString, $"Data Source={_dbPath}");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddXUnit(testOutputHelper);
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
                File.Delete(_dbPath);
            }
            catch (Exception e)
            {
                var logger = Services.GetRequiredService<ILogger<DemoAppFactory>>();
                logger.LogError(e, "Failed to delete test database directory at {DbPath}", _dbPath);
                throw;
            }
        }

        base.Dispose(disposing);
    }
}
