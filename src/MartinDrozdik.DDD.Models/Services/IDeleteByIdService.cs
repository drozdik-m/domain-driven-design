using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Services;

/// <summary>
/// Represents a service that deletes an instance by its key.
/// </summary>
/// <typeparam name="TIdentity">Type of the deleted item.</typeparam>
public interface IDeleteByIdService<in TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Deletes an item by its key.
    /// </summary>
    /// <param name="id">Identifier of the deleted item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task with a success or an <see cref="Error"/>.</returns>
    Task<IUnitResult<Error>> DeleteAsync(TIdentity id, CancellationToken cancellationToken);
}
