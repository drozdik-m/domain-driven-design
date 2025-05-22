using System.Diagnostics;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Identities;

/// <inheritdoc cref="IIdentity{TValue}"/>
/// <typeparam name="TKey">Actual value of the ID.</typeparam>
/// <typeparam name="TSelf">Self-referencing generic type.</typeparam>
[DebuggerDisplay("{Key}")]
public abstract class Identity<TSelf, TKey> : ValueObject, IIdentity<TKey>
    where TSelf : Identity<TSelf, TKey>, new()
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Identity{TSelf, TValue}"/> class.
    /// </summary>
    protected Identity()
    {
    }

    /// <inheritdoc />
    public required TKey Key { get; init; }

    /// <summary>
    /// Creates a new instance of the strongly typed ID.
    /// </summary>
    /// <param name="key">The actual value of the new identifier.</param>
    /// <returns>New <typeparamref name="TSelf"/>.</returns>
    public static TSelf Create(TKey key)
    {
        return new TSelf
        {
            Key = key,
        };
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }
}
