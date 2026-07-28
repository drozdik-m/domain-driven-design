using System.Linq.Expressions;
using System.Security.Claims;
using MartinDrozdik.DDD.Disposing;
using MartinDrozdik.DDD.Testing.Logging;
using MartinDrozdik.DDD.Web.Environments;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
    private string _environment = AppEnvironments.Testing;
    private ClaimsPrincipal? _claimsPrincipal;

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
    public TestedAppBuilder<TProgram> With(Action<IWebHostBuilder> config)
    {
        _configs.Add(config);
        return this;
    }

    /// <summary>
    /// Adds an option configuration for the <see cref="IWebHostBuilder"/>.
    /// Options are applied BEFORE "Program.cs" startup.
    /// </summary>
    /// <param name="key">Key of the option to set.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithOption(string key, string value)
    {
        return With(e => e.UseSetting(key, value));
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
        return With(e => e.SetOption(propertySelector, value));
    }

    /// <summary>
    /// Adds service configuration for the <see cref="IWebHostBuilder"/>.
    /// </summary>
    /// <param name="config">The action to extend services collection.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithServices(Action<IServiceCollection> config)
    {
        return With(builder => builder.ConfigureServices(config));
    }

    /// <summary>
    /// Registers a <see cref="TestLogger"/> capturing everything the application logs, so a test can assert on it.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="WithTestingLogger(out TestLogger)"/> overload to get hold of the logger directly.
    /// <para>
    /// The application logs at <see cref="LogLevel.Information"/> and above by default, so entries below that level never reach the logger.
    /// </para>
    /// </remarks>
    /// <param name="testLogger">The <see cref="TestLogger"/> instance to use for logging.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithTestingLogger(TestLogger testLogger)
    {
        return WithServices(services => services.AddSingleton<ILoggerProvider>(testLogger));
    }

    /// <summary>
    /// Registers a <see cref="TestLogger"/> capturing everything the application logs and hands it out,
    /// so a test can assert on it without resolving it from the application.
    /// </summary>
    /// <param name="testingLogger">The registered logger, recording from the moment the application starts.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithTestingLogger(out TestLogger testingLogger)
    {
        testingLogger = new TestLogger();
        return WithTestingLogger(testingLogger);
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
    /// Sets a <see cref="FakeTimeProvider"/> for the application.
    /// </summary>
    /// <param name="fakeTimeProvider">The used fake time provider.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithFakeTime(FakeTimeProvider fakeTimeProvider)
    {
        return WithServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(fakeTimeProvider);
        });
    }

    /// <summary>
    /// Sets a fake <see cref="TimeProvider"/> for the application.
    /// </summary>
    /// <param name="fakeTime">Static fake time shared across the application.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithFakeTime(DateTimeOffset fakeTime)
    {
        var fakeTimeProvider = new FakeTimeProvider(fakeTime);
        return WithFakeTime(fakeTimeProvider);
    }

    /// <summary>
    /// Sets a determined <see cref="ClaimsPrincipal"/> for the application.
    /// </summary>
    /// <param name="claimsPrincipal">The new claims principal.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
    {
        _claimsPrincipal = claimsPrincipal;
        return this;
    }

    /// <summary>
    /// Sets a determined <see cref="ClaimsPrincipal"/> for the application from provided user with roles.
    /// </summary>
    /// <param name="userId">Id of the user.</param>
    /// <param name="roles">Roles of the user.</param>
    /// <returns>This.</returns>
    public TestedAppBuilder<TProgram> WithUserAndRoles(string userId, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return WithClaimsPrincipal(claimsPrincipal);
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
            ClaimsPrincipal = _claimsPrincipal,
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
