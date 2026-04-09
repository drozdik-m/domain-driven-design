using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Factory for bootstraping an application in memory for integration testing, with additional features such as logging to xUnit output and easy registration of test endpoints.
/// </summary>
/// <typeparam name="TProgram">Type of the entrypoint Program.cs.</typeparam>
public sealed class TestedApp<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    /// <summary>
    /// List of created scopes to dispose of after the test execution.
    /// </summary>
    private readonly IList<IDisposable> _scopes = [];

    /// <summary>
    /// Options for configuring this factory, including logging, additional configuration and endpoints.
    /// </summary>
    private readonly TestedAppOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestedApp{TProgram}"/> class.
    /// </summary>
    /// <param name="options">More detailed options of this factory.</param>
    public TestedApp(TestedAppOptions options)
    {
        _options = options;
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
            logging.AddXUnit(_options.TestOutputHelper);
        });

        // Change environment to for registering test-specific configuration
        // Also behaves more like a producation app than development environment
        builder.UseEnvironment("Testing");

        // Register test endpoints via IStartupFilter
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new EndpointStartupFilter(_options.EndpointConfig)));

        // Invoke user-provided additional configuration if available
        _options.Config.Invoke(builder);
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

            _scopes.Clear();

            foreach (var disposable in _options.Disposables)
            {
                disposable.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Hooks into pipeline construction to append endpoint registrations.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    private sealed class EndpointStartupFilter(Action<IEndpointRouteBuilder> configure)
        : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                next(app); // Run the app's normal startup first
                app.UseEndpoints(configure);
            };
    }
}
