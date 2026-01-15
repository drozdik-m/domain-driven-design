using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Services;

/// <summary>
/// Represents a service that saves an item.
/// </summary>
/// <typeparam name="TIdentity">Type of the items' ID.</typeparam>
/// <typeparam name="TItem">Type of the saved item.</typeparam>
public interface ISaveService<TIdentity, in TItem>
    where TIdentity : notnull
    where TItem : IIdentifiable<TIdentity>
{
    /// <summary>
    /// Upserts an item.
    /// The operation is idempotent - upserting the same item multiple times has the same effect as upserting it once.
    /// </summary>
    /// <param name="item">The item to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/> with the items' ID.</returns>
    Task<TIdentity> SaveAsync(TItem item, CancellationToken cancellationToken);
}
