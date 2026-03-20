using System.Linq.Expressions;
using MartinDrozdik.DDD.Testing.Disposing;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Builder for creating <see cref="TestWebApplicationFactory{TProgram}"/>.
/// </summary>
/// <typeparam name="TProgram">Type of the tested program.</typeparam>
public abstract class TestWebApplicationFactoryBuilder<TProgram>
    where TProgram : class
{
    private readonly List<Action<IWebHostBuilder>> _configs = [];
    private readonly List<Action<IEndpointRouteBuilder>> _endpointConfigs = [];
    private readonly List<IDisposable> _disposables = [];
    private ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestWebApplicationFactoryBuilder{TProgram}"/> class.
    /// </summary>
    /// <param name="testOutputHelper">Used <see cref="ITestOutputHelper"/>.</param>
    protected TestWebApplicationFactoryBuilder(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// Sets the required <see cref="ITestOutputHelper"/> for logging.
    /// </summary>
    /// <param name="testOutputHelper">Used <see cref="ITestOutputHelper"/>.</param>
    /// <returns>This.</returns>
    public TestWebApplicationFactoryBuilder<TProgram> WithOutput(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        return this;
    }

    /// <summary>
    /// Adds a configuration action for the <see cref="IWebHostBuilder"/>.
    /// Multiple calls will add multiple configurations, which will be executed in the order they were added.
    /// </summary>
    /// <param name="config">The added configuration.</param>
    /// <returns>This.</returns>
    public TestWebApplicationFactoryBuilder<TProgram> WithConfig(Action<IWebHostBuilder> config)
    {
        _configs.Add(config);
        return this;
    }

    /// <summary>
    /// Adds an option configuration for the <see cref="IWebHostBuilder"/>.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="propertySelector">Expression to select the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>This.</returns>
    public TestWebApplicationFactoryBuilder<TProgram> WithOption<TOptions>(Expression<Func<TOptions, object>> propertySelector, string value)
        where TOptions : IAppOptions
    {
        return WithConfig(e => e.SetOption(propertySelector, value));
    }

    /// <summary>
    /// Adds service configuration for the <see cref="IWebHostBuilder"/>.
    /// </summary>
    /// <param name="config">The action to extend services collection.</param>
    /// <returns>This.</returns>
    public TestWebApplicationFactoryBuilder<TProgram> WithServices(Action<IServiceCollection> config)
    {
        return WithConfig(builder => builder.ConfigureServices(config));
    }

    /// <summary>
    /// Adds a configuration action for the <see cref="IEndpointRouteBuilder"/>.
    /// Multiple calls will add multiple configurations, which will be executed in the order they were added.
    /// </summary>
    /// <param name="endpointConfig">The added endpoint configuration.</param>
    /// <returns>This.</returns>
    public TestWebApplicationFactoryBuilder<TProgram> WithEndpoints(Action<IEndpointRouteBuilder> endpointConfig)
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
    public TestWebApplicationFactoryBuilder<TProgram> WithDisposable(IDisposable disposable)
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
    public TestWebApplicationFactoryBuilder<TProgram> WithDisposable(Action disposeAction)
    {
        return WithDisposable(new DisposableAction(disposeAction));
    }

    /// <summary>
    /// Builds the resulting <see cref="TestWebApplicationFactory{TProgram}"/> with the provided configurations.
    /// </summary>
    /// <remarks>
    /// Don't forget to dispose it.
    /// </remarks>
    /// <returns>New <see cref="TestWebApplicationFactory{TProgram}"/>.</returns>
    public TestWebApplicationFactory<TProgram> Build()
    {
        if (_testOutputHelper is null)
        {
            throw new InvalidOperationException($"Output helper is required. Call {nameof(WithOutput)} before building.");
        }

        var configsCopy = new List<Action<IWebHostBuilder>>(_configs);
        var endpointsCopy = new List<Action<IEndpointRouteBuilder>>(_endpointConfigs);
        var options = new TestWebApplicationFactoryOptions
        {
            TestOutputHelper = _testOutputHelper,
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

        return new TestWebApplicationFactory<TProgram>(options);
    }
}
