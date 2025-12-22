namespace MartinDrozdik.DDD.Models.Templates;

/// <summary>
/// An entity that represents an object which identity is defined by its Id.
/// </summary>
/// <remarks>
/// In constrast to value objects, domain entities have an identity that distinguishes them from other entities.
/// We are keeping this as an interface for more flexibility in certain scenarios, especially when working with ORMs that may require parameterless constructors or specific inheritance structures.
/// </remarks>
/// <typeparam name="TIdentity">The type of the entities' identity.</typeparam>
public interface IDomainEntity<out TIdentity> : IIdentifiable<TIdentity>
    where TIdentity : notnull
{
}
