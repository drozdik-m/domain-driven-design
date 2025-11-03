namespace MartinDrozdik.DDD.Models.Templates;

/// <summary>
/// An aggregate that represents a single usecase unit in a domain-driven design context.
/// </summary>
/// <typeparam name="TIdentity">The type of the aggregates' identity.</typeparam>
public abstract class AggregateRoot<TIdentity> : DomainEntity<TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TIdentity}"/> class.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    protected AggregateRoot(TIdentity id)
        : base(id)
    {
    }
}
