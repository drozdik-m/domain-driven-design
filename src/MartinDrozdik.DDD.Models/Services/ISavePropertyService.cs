using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Services;

/// <summary>
/// Represents a service that saves a property of an aggregate.
/// For example, updates the visibility of an aggregate.
/// </summary>
/// <typeparam name="TIdentity">Type of the identifier of the aggregate.</typeparam>
/// <typeparam name="TProperty">Type of the saved property.</typeparam>
public interface ISavePropertyService<in TIdentity, in TProperty>
    where TIdentity : notnull
{
    /// <summary>
    /// If the agregate exists, saves its property.
    /// </summary>
    /// <param name="identity">Identifier of the aggregate.</param>
    /// <param name="property">Property to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task with a success or an <see cref="Error"/>.</returns>
    Task<IUnitResult<Error>> SaveAsync(TIdentity identity, TProperty property, CancellationToken cancellationToken);
}
