using System.Net.Mime;
using System.Text;
using MartinDrozdik.DDD.Testing.Attributes;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Smoke;

/// <summary>
/// Base class for smoke tests of ASP.NET Core web applications.
/// </summary>
/// <typeparam name="TProgram">Type of the app entrypoint class.</typeparam>
/// <param name="builder">App factory under test.</param>
public class EndpointSmokeTester<TProgram>(TestedAppBuilder<TProgram> builder)
    : IDisposable
    where TProgram : class
{
    private readonly TestedApp<TProgram> _factory = builder.Build();

    /// <summary>
    /// Smoke-tests an endpoint.
    /// </summary>
    /// <param name="testCase">What endpoint to test with what parameters.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    [AssertionMethod]
    public async Task Test(EndpointTest testCase, CancellationToken cancellationToken)
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent(
            testCase.Content ?? string.Empty,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        HttpResponseMessage response;
        if (testCase.Method == HttpMethod.Get)
        {
            response = await client.GetAsync(testCase.Url, cancellationToken);
        }
        else if (testCase.Method == HttpMethod.Post)
        {
            response = await client.PostAsync(testCase.Url, content, cancellationToken);
        }
        else if (testCase.Method == HttpMethod.Put)
        {
            response = await client.PutAsync(testCase.Url, content, cancellationToken);
        }
        else if (testCase.Method == HttpMethod.Patch)
        {
            response = await client.PatchAsync(testCase.Url, content, cancellationToken);
        }
        else if (testCase.Method == HttpMethod.Delete)
        {
            response = await client.DeleteAsync(testCase.Url, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"HTTP method {testCase.Method} is not supported in {nameof(EndpointSmokeTester<>)}.{nameof(Test)}.");
        }

        // Assert
        Assert.True(
            response.IsSuccessStatusCode || testCase.AcceptableCodes.Contains(response.StatusCode),
            $"Endpoint {testCase} returned status code {(int)response.StatusCode} ({response.ReasonPhrase}), which is not acceptable.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }
    }
}
