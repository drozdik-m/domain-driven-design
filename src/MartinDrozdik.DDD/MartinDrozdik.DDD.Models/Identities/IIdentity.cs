namespace MartinDrozdik.DDD.Models.Identities;

/// <summary>
/// Strongly typed identifier for domain entities and aggregates.
/// </summary>
/// <typeparam name="TKey">Actual value of the ID.</typeparam>
public interface IIdentity<out TKey>
    where TKey : notnull
{
    /// <summary>
    /// Gets the actual value of the identifier.
    /// </summary>
    TKey Key { get; }
}
