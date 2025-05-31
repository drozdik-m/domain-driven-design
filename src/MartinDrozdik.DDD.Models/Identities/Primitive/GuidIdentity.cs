namespace MartinDrozdik.DDD.Models.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class GuidIdentity<TSelf> : Identity<TSelf, Guid>
    where TSelf : GuidIdentity<TSelf>, new();
