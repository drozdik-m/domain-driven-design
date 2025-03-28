namespace MartinDrozdik.DDD.Models.Services;

/// <summary>
/// Represents a service that can be seeded.
/// </summary>
public interface ISeedableService
{
    /// <summary>
    /// Seeds the service with predefined instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task.</returns>
    Task SeedAsync(CancellationToken cancellationToken);
}
