using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Demo;

/// <summary>
/// Extensions for <see cref="HostApplicationBuilder"/> for demo purposes.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Configures services for the demo application.
    /// </summary>
    public static HostApplicationBuilder ConfigureDemo(this HostApplicationBuilder hostApplicationBuilder)
    {
        return hostApplicationBuilder;
    }
}
