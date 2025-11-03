namespace MartinDrozdik.DDD.Models.Templates;

/// <summary>
/// An entity that represents an object which identity is defined by its Id.
/// </summary>
/// <remarks>
/// In constrast to value objects, domain entities have an identity that distinguishes them from other entities.
/// </remarks>
/// <typeparam name="TIdentity">The type of the entities' identity.</typeparam>
public abstract class DomainEntity<TIdentity> : IIdentifiable<TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEntity{TIdentity}"/> class.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    protected DomainEntity(TIdentity id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets or sets the identity identifier of this entity.
    /// </summary>
    public TIdentity Id { get; protected set; }
}
