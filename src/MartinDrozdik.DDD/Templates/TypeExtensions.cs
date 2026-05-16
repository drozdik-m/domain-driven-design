namespace MartinDrozdik.DDD.Templates;

/// <summary>
/// Extensions for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Checks if the given type implements the <see cref="IAggregateRoot{TIdentity}"/> interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements the interface, otherwise false.</returns>
    public static bool IsAggregateRoot(this Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
    }

    /// <summary>
    /// Checks if the given type implements the <see cref="IDomainEntity{TIdentity}"/> interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements the interface, otherwise false.</returns>
    public static bool IsDomainEntity(this Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEntity<>));
    }
}
