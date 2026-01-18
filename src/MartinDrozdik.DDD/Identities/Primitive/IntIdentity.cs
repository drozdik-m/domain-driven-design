using MartinDrozdik.DDD.Identities;

namespace MartinDrozdik.DDD.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class IntIdentity<TSelf>(int key) : Identity<TSelf, int>(key)
    where TSelf : IntIdentity<TSelf>, new()
{
}
