namespace MartinDrozdik.DDD.Models;

/// <summary>
/// Represents an item that can be identified by a key.
/// </summary>
/// <typeparam name="TIdentity">Type of the key.</typeparam>
public interface IIdentifiable<out TIdentity>
    where TIdentity : notnull
{
    /// <summary>
    /// Gets the identifier of this item.
    /// </summary>
    public TIdentity Id { get; }
}
