namespace MartinDrozdik.DDD.Templates;

/// <summary>
/// An aggregate that represents a single usecase unit in a domain-driven design context.
/// </summary>
/// <remarks>
/// We are keeping this as an interface for more flexibility in certain scenarios, especially when working with ORMs that may require parameterless constructors or specific inheritance structures.
/// </remarks>
/// <typeparam name="TIdentity">The type of the aggregates' identity.</typeparam>
public interface IAggregateRoot<out TIdentity> : IDomainEntity<TIdentity>
    where TIdentity : notnull
{
}
