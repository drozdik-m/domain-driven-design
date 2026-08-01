using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// An application factory under test.
/// </summary>
public interface ITestedApp
{
    /// <summary>
    /// Gets the environment this factory is configured to use.
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the current <see cref="ITestOutputHelper"/> for xUnit output logging.
    /// </summary>
    ITestOutputHelper TestOutputHelper { get; }

    /// <summary>
    /// Gets the root <see cref="IServiceProvider"/> of the running application.
    /// </summary>
    /// <remarks>
    /// This is the root provider, so resolving a scoped service directly from it is an error.
    /// Use <see cref="TestedApp{TProgram}.GetScopedService{TService}"/> or open a scope explicitly.
    /// </remarks>
    IServiceProvider Services { get; }

    /// <summary>
    /// Creates and configures a new <see cref="HttpClient"/> for sending requests to the application. The client is configured with the base address of the application.
    /// </summary>
    /// <remarks>
    /// Dispose the returned <see cref="HttpClient"/> when no longer needed.
    /// For request-heavy scenarios, prefer reusing clients or using an <see cref="IHttpClientFactory"/> to avoid socket exhaustion.
    /// </remarks>
    /// <returns>A configured <see cref="HttpClient"/>.</returns>
    HttpClient CreateClient();
}
