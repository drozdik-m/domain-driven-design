using System.Diagnostics;

namespace MartinDrozdik.DDD.Models.Identities;

/// <inheritdoc cref="IIdentity{TSelf, TValue}"/>
[DebuggerDisplay("{Value}")]
public abstract class Identity<TSelf, TValue> : ValueObject, IIdentity<TSelf, TValue>
    where TSelf : Identity<TSelf, TValue>, new()
    where TValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Identity{TSelf, TValue}"/> class.
    /// </summary>
    protected Identity()
    {
    }

    /// <inheritdoc />
    public required TValue Value { get; init; }

    /// <inheritdoc cref="IIdentity{TSelf, TValue}.Create(TValue)"/>
    public static TSelf Create(TValue value)
    {
        return new TSelf
        {
            Value = value,
        };
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}