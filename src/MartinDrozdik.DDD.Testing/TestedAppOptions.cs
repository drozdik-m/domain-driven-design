using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Setup options for new instance of <see cref="TestedApp{TProgram}"/>.
/// </summary>
public class TestedAppOptions
{
    /// <summary>
    /// Gets the required <see cref="ITestOutputHelper"/> for logging.
    /// </summary>
    public required ITestOutputHelper TestOutputHelper { get; init; }

    /// <summary>
    /// Gets additional configuration for the web host builder.
    /// </summary>
    public Action<IWebHostBuilder> Config { get; init; } = _ => { };

    /// <summary>
    /// Gets configuration of extra endpoints.
    /// </summary>
    public Action<IEndpointRouteBuilder> EndpointConfig { get; init; } = _ => { };

    /// <summary>
    /// Gets list of dependencies that need to be disposed of after the test execution.
    /// </summary>
    public IEnumerable<IDisposable> Disposables { get; init; } = [];
}
