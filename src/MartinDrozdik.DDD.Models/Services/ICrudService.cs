using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Services;

/// <summary>
/// Service combining CRUD operations.
/// </summary>
/// <typeparam name="TEntity">Type of the operated item.</typeparam>
/// <typeparam name="TIdentity">Identifiers of the item.</typeparam>
public interface ICrudService<TEntity, TIdentity>
    : ISaveService<TIdentity, TEntity>,
    IDeleteByIdService<TIdentity>,
    IGetAllService<TEntity>,
    IGetByIdService<TEntity, TIdentity>
    where TIdentity : notnull
    where TEntity : IIdentifiable<TIdentity>
{
}
