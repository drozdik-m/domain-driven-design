using System.Reflection;
using System.Text.Json;
using MartinDrozdik.DDD.Testing.Endpoints;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace MartinDrozdik.DDD.Testing.Smoke;

public record Endpoint(HttpMethod Method, string Url);

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
