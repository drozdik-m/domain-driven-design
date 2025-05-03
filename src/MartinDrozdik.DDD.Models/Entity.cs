namespace MartinDrozdik.DDD.Models;

/// <summary>
/// An entity that represents an object which identity is defined by its Id.
/// </summary>
/// <remarks>
/// In constrast to value objects, entities have an identity that distinguishes them from other entities.
/// </remarks>
/// <typeparam name="TIdentity">The type of the entities' identity.</typeparam>
public abstract class Entity<TIdentity> : IIdentifiable<TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TIdentity}"/> class.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    protected Entity(TIdentity id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets or sets the identity identifier of this entity.
    /// </summary>
    public TIdentity Id { get; protected set; }
}
