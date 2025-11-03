namespace MartinDrozdik.DDD.Models.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class GuidIdentity<TSelf>(Guid key) : Identity<TSelf, Guid>(key)
    where TSelf : GuidIdentity<TSelf>;
