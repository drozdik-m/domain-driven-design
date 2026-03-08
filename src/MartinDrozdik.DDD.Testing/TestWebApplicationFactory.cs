using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Represents a base class for creating a test web application factory for integration testing.
/// </summary>
/// <typeparam name="TProgram">Type of the entrypoint Program.cs.</typeparam>
/// <param name="testOutputHelper">Output helper for logging output.</param>
public class TestWebApplicationFactory<TProgram>(ITestOutputHelper testOutputHelper) : WebApplicationFactory<TProgram>
    where TProgram : class
{
    /// <summary>
    /// Additional configuration for the web host builder.
    /// </summary>
    private readonly Action<IWebHostBuilder>? _config;

    /// <summary>
    /// List of created scopes to dispose of after the test execution.
    /// </summary>
    private readonly IList<IDisposable> _scopes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TestWebApplicationFactory{TProgram}"/> class.
    /// </summary>
    /// <param name="testOutputHelper">Output helper for logging output.</param>
    /// <param name="config">Additional configuration for the web host builder.</param>
    public TestWebApplicationFactory(ITestOutputHelper testOutputHelper, Action<IWebHostBuilder> config)
        : this(testOutputHelper)
    {
        _config = config;
    }

    /// <summary>
    /// Creates a new scope and resolves the specified service type from the service provider.
    /// Scopes created by this method are tracked and disposed of when the factory is disposed.
    /// </summary>
    /// <typeparam name="TService">Type ofthe requested service.</typeparam>
    /// <returns>The requested service.</returns>
    public TService GetScopedService<TService>()
        where TService : class
    {
        var scope = Services.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<TService>();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Configures information logging to the xUnit output
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddXUnit(testOutputHelper);
        });

        // Invoke user-provided additional configuration if available
        _config?.Invoke(builder);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var scope in _scopes)
            {
                scope.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
