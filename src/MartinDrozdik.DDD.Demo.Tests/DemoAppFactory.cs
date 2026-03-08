using System.Threading;
using MartinDrozdik.DDD.Demo.Client.Generated;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests;

public class DemoAppFactory(ITestOutputHelper testOutputHelper)
    : TestWebApplicationFactory<Program>(testOutputHelper)
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test.db");

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
        base.ConfigureWebHost(builder);
        builder.SetOption<DatabaseOptions>(e => e.ConnectionString, $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
