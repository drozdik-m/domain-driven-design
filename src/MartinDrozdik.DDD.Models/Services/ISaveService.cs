using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

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
    /// </summary>
    /// <param name="item">The item to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task with the items' ID or an <see cref="Error"/>.</returns>
    Task<IResult<TIdentity, Error>> SaveAsync(TItem item, CancellationToken cancellationToken);
}
