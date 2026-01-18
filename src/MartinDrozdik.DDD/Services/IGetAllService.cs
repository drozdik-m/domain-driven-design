namespace MartinDrozdik.DDD.Services;

/// <summary>
/// Represents a service that gets all saved items.
/// </summary>
/// <typeparam name="TItem">Type of the returned item.</typeparam>
public interface IGetAllService<TItem>
{
    /// <summary>
    /// Gets all saved items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task with all result aggregates.</returns>
    Task<IEnumerable<TItem>> GetAllAsync(CancellationToken cancellationToken);
}
