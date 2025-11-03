using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Demo;

/// <summary>
/// Host of this demo application.
/// </summary>
public class DemoHost
{
    private readonly IHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoHost"/> class.
    /// </summary>
    public DemoHost(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Creates a default instance of <see cref="DemoHost"/>.
    /// </summary>
    /// <returns>The created <see cref="DemoHost"/> instance.</returns>
    public static DemoHost CreateDefault()
    {
        var app = Host.CreateApplicationBuilder();
        app.ConfigureDemo();
        var host = app.Build();
        return new DemoHost(host);
    }
}
