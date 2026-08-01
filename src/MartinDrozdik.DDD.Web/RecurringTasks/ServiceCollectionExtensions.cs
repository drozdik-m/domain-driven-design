using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Removes every registered recurring task loop, leaving all other hosted services alone.
    /// </summary>
    /// <remarks>
    /// Handy in integration tests and in environments where background work is unwanted, when disabling each task individually would be tedious.
    /// The tasks themselves stay registered and resolvable, only their loops are gone.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to extend.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection RemoveRecurringTasks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var hosts = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType is { IsGenericType: true } implementationType
                && implementationType.GetGenericTypeDefinition() == typeof(RecurringTaskHost<>))
            .ToList();

        foreach (var host in hosts)
        {
            services.Remove(host);
        }

        return services;
    }
}
