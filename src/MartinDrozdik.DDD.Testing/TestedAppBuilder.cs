using System.Linq.Expressions;
using MartinDrozdik.DDD.Disposing;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Builder for creating <see cref="TestedApp{TProgram}"/>.
/// </summary>
/// <typeparam name="TProgram">Type of the tested program.</typeparam>
/// <param name="testOutputHelper">Used <see cref="ITestOutputHelper"/>.</param>
public abstract class TestedAppBuilder<TProgram>(ITestOutputHelper testOutputHelper)
    where TProgram : class
{
    private readonly List<Action<IWebHostBuilder>> _configs = [];
    private readonly List<Action<IEndpointRouteBuilder>> _endpointConfigs = [];
    private readonly List<IDisposable> _disposables = [];
    private string _environment = "Testing";

    /// <summary>
    /// Sets the applications environment.
    /// </summary>
    /// <remarks>
    /// It is recommended to set some non-default environment (e.g. "Testing") to avoid confusion with development environment and to allow environment-specific configurations.
    /// </remarks>
    /// <param name="newEnvironment">New environment.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithOutput(string newEnvironment)
    {
        _environment = newEnvironment;
        return this;
    }

    /// <summary>
    /// Sets the required <see cref="ITestOutputHelper"/> for logging.
    /// </summary>
    /// <param name="newTestOutputHelper">Used <see cref="ITestOutputHelper"/>.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithOutput(ITestOutputHelper newTestOutputHelper)
    {
        testOutputHelper = newTestOutputHelper;
        return this;
    }

    /// <summary>
    /// Adds a configuration action for the <see cref="IWebHostBuilder"/>.
    /// Configuration is applied BEFORE "Program.cs" startup.
    /// Multiple calls will add multiple configurations, which will be executed in the order they were added.
    /// </summary>
    /// <param name="config">The added configuration.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithConfig(Action<IWebHostBuilder> config)
    {
        _configs.Add(config);
        return this;
    }

    /// <summary>
    /// Adds an option configuration for the <see cref="IWebHostBuilder"/>.
    /// Options are applied BEFORE "Program.cs" startup.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="propertySelector">Expression to select the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithOption<TOptions>(Expression<Func<TOptions, object>> propertySelector, string value)
        where TOptions : IAppOptions
    {
        return WithConfig(e => e.SetOption(propertySelector, value));
    }

    /// <summary>
    /// Adds service configuration for the <see cref="IWebHostBuilder"/>.
    /// </summary>
    /// <param name="config">The action to extend services collection.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithServices(Action<IServiceCollection> config)
    {
        return WithConfig(builder => builder.ConfigureServices(config));
    }

    /// <summary>
    /// Adds a configuration action for the <see cref="IEndpointRouteBuilder"/>.
    /// Multiple calls will add multiple configurations, which will be executed in the order they were added.
    /// </summary>
    /// <param name="endpointConfig">The added endpoint configuration.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithEndpoints(Action<IEndpointRouteBuilder> endpointConfig)
    {
        _endpointConfigs.Add(endpointConfig);
        return this;
    }

    /// <summary>
    /// Adds a disposable dependency that will be disposed along the factory
    /// Multiple calls will add multiple disposables.
    /// </summary>
    /// <param name="disposable">The added dependency.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
        return this;
    }

    /// <summary>
    /// Adds a disposable dependency that will be disposed along the factory
    /// Multiple calls will add multiple disposables.
    /// </summary>
    /// <param name="disposeAction">The action to be exectuted at disposal.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithDisposable(Action disposeAction)
    {
        return WithDisposable(new DisposableAction(disposeAction));
    }

    /// <summary>
    /// Sets a fake <see cref="TimeProvider"/> for the application.
    /// </summary>
    /// <param name="fakeTime">Static fake time shared across the application.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithFakeTime(DateTimeOffset fakeTime)
    {
        var fakeTimeProvider = new FakeTimeProvider(fakeTime);
        return WithServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(fakeTimeProvider);
        });
    }

    /// <summary>
    /// Builds the resulting <see cref="TestedApp{TProgram}"/> with the provided configurations.
    /// </summary>
    /// <remarks>
    /// Don't forget to dispose it.
    /// </remarks>
    /// <returns>New <see cref="TestedApp{TProgram}"/>.</returns>
    public TestedApp<TProgram> Build()
    {
        if (testOutputHelper is null)
        {
            throw new InvalidOperationException($"Output helper is required. Call {nameof(WithOutput)} before building.");
        }

        var configsCopy = new List<Action<IWebHostBuilder>>(_configs);
        var endpointsCopy = new List<Action<IEndpointRouteBuilder>>(_endpointConfigs);
        var options = new TestedAppOptions
        {
            Environment = _environment,
            TestOutputHelper = testOutputHelper,
            Config = builder =>
            {
                foreach (var config in configsCopy)
                {
                    config(builder);
                }
            },
            EndpointConfig = endpoints =>
            {
                foreach (var endpointConfig in endpointsCopy)
                {
                    endpointConfig(endpoints);
                }
            },
            Disposables = _disposables.ToArray(),
        };

        return new TestedApp<TProgram>(options);
    }
}
