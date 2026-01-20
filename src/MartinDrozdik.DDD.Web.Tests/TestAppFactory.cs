using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestAppFactory(ITestOutputHelper testOutputHelper) : WebApplicationFactory<TestProgram>
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private readonly Action<IWebHostBuilder>? _config;

    public TestAppFactory(ITestOutputHelper testOutputHelper, Action<IWebHostBuilder> config)
        : this(testOutputHelper)
    {
        _config = config;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddXUnit(_testOutputHelper);
        });

        _config?.Invoke(builder);
    }
}
