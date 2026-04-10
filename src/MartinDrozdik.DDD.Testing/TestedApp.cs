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
/// <param name="options">More detailed options of this factory.</param>
public sealed class TestedApp<TProgram>(TestedAppOptions options) : WebApplicationFactory<TProgram>
    where TProgram : class
{
    /// <summary>
    /// List of created scopes to dispose of after the test execution.
    /// </summary>
    private readonly IList<IDisposable> _scopes = [];

    /// <summary>
    /// Gets the environment this factory is configured to use.
    /// </summary>
    public string Environment => options.Environment;

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
            logging.AddXUnit(options.TestOutputHelper);
        });

        // Change environment to for registering test-specific configuration
        // Also behaves more like a producation app than development environment
        builder.UseEnvironment(options.Environment);

        // Register test endpoints via IStartupFilter
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new EndpointStartupFilter(options.EndpointConfig)));

        // Invoke user-provided additional configuration if available
        options.Config.Invoke(builder);
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

            foreach (var disposable in options.Disposables)
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
