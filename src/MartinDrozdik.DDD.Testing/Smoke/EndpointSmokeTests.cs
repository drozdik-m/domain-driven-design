using System.Net;
using System.Reflection;
using System.Text.Json;
using MartinDrozdik.DDD.Testing.Endpoints;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace MartinDrozdik.DDD.Testing.Smoke;

public record EndpointTest(HttpMethod Method, string Url)
{
    /// <summary>
    /// Gets acceptable status codes for this endpoint test.
    /// Besides 2xx status codes, can we consider other status codes as acceptable?
    /// Smoke tests are not meant to be strict, so we can allow some flexibility here.
    /// </summary>
    /// <example>
    /// If an endpoint is protected by authentication, we might expect 401 or 403 status codes, and that would be perfectly fine for a smoke test.
    /// </example>
    public List<HttpStatusCode> AcceptableCodes { get; init; } = [];

    public EndpointTest WithAcceptableCode(HttpStatusCode code)
    {
        return new EndpointTest(Method, Url)
        {
            AcceptableCodes = [.. AcceptableCodes, code],
        };
    }
}

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

    /*public Task Test()
    {
    
    }*/

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
