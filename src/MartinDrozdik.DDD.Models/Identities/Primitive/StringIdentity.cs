namespace MartinDrozdik.DDD.Models.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class StringIdentity<TSelf> : Identity<TSelf, string>
    where TSelf : StringIdentity<TSelf>, new()
{
}