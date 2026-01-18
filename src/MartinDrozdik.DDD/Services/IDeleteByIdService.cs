namespace MartinDrozdik.DDD.Services;

/// <summary>
/// Represents a service that deletes an instance by its key.
/// </summary>
/// <typeparam name="TIdentity">Type of the deleted item.</typeparam>
public interface IDeleteByIdService<in TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Deletes an item by its key.
    /// The operation is idempotent – deleting a non-existing item is considered a success.
    /// </summary>
    /// <param name="id">Identifier of the deleted item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task DeleteAsync(TIdentity id, CancellationToken cancellationToken);
}
