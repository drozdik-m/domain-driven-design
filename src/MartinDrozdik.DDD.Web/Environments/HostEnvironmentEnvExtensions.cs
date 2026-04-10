using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Environments;

/// <summary>
/// Extensions for <see cref="IHostEnvironment"/>.
/// </summary>
public static class HostEnvironmentEnvExtensions
{
    /// <summary>
    /// Checks if the current host environment name is <see cref="AppEnvironments.Testing"/>.
    /// </summary>
    /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
    /// <returns><see langword="true"/> if the environment name is <see cref="AppEnvironments.Testing"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsTesting(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsEnvironment(AppEnvironments.Testing);
    }
}
