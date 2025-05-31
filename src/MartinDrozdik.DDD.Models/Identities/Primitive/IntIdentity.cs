namespace MartinDrozdik.DDD.Models.Identities.Primitive;

/// <inheritdoc cref="Identity{TSelf, TValue}"/>
public abstract class IntIdentity<TSelf> : Identity<TSelf, int>
    where TSelf : IntIdentity<TSelf>, new();
