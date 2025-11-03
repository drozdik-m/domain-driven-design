using System.Diagnostics;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Identities;

/// <inheritdoc cref="IIdentity{TValue}"/>
/// <typeparam name="TKey">Actual value of the ID.</typeparam>
/// <typeparam name="TSelf">Self-referencing generic type.</typeparam>
[DebuggerDisplay("{Key}")]
public abstract class Identity<TSelf, TKey> : ValueObject, IIdentity<TKey>
    where TSelf : Identity<TSelf, TKey>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Identity{TSelf, TValue}"/> class.
    /// </summary>
    /// <param name="key">The actual value of the identifier.</param>
    protected Identity(TKey key)
    {
        Key = key;
    }

    /// <inheritdoc />
    public TKey Key { get; }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }
}
