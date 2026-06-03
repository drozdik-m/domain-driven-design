namespace MartinDrozdik.DDD.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class StringIdentity<TSelf>(string key) : Identity<TSelf, string>(key)
    where TSelf : StringIdentity<TSelf>
{
}
