using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Errors;

namespace MartinDrozdik.DDD.Services;

/// <summary>
/// Service for getting specific item.
/// </summary>
/// <typeparam name="TItem">Type of the returned item.</typeparam>
/// <typeparam name="TIdentity">The identifier of the returned item.</typeparam>
public interface IGetByIdService<TItem, TIdentity>
{
    /// <summary>
    /// Gets a specific item.
    /// </summary>
    /// <param name="id">ID of target item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Target item or an <see cref="Error"/>.</returns>
    Task<IResult<TItem, Error>> GetAsync(TIdentity id, CancellationToken cancellationToken);
}
