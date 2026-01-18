namespace MartinDrozdik.DDD.Services;

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
    /// The operation is idempotent - saving the same property value multiple times has the same effect as saving it once.
    /// </summary>
    /// <param name="identity">Identifier of the aggregate.</param>
    /// <param name="property">Property to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task SaveAsync(TIdentity identity, TProperty property, CancellationToken cancellationToken);
}
