namespace MartinDrozdik.DDD.Models.Identities;

/// <summary>
/// Strongly typed identifier for domain entities and aggregates.
/// </summary>
/// <typeparam name="TSelf">Self-referencing generic type.</typeparam>
/// <typeparam name="TValue">Actual value of the ID.</typeparam>
public interface IIdentity<TSelf, TValue>
    where TSelf : IIdentity<TSelf, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Gets the actual value of the identifier.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Creates a new instance of the strongly typed ID.
    /// </summary>
    /// <param name="value">The actual value of the new identifier.</param>
    /// <returns>New <typeparamref name="TSelf"/>.</returns>
    static abstract TSelf Create(TValue value);
}