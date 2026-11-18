using System.Linq.Expressions;

namespace MartinDrozdik.DDD.Identities.Converters;

/// <summary>
/// Provides factory and conversion expressions for Identity types.
/// </summary>
/// <remarks>
/// Aims to provide easy integration with Entity Framework and LINQ.
/// </remarks>
/// <typeparam name="TIdentity">The <see cref="Identity{TSelf, TKey}"/> type.</typeparam>
/// <typeparam name="TKey">The underlying key type.</typeparam>
public class IdentityConverter<TIdentity, TKey>
    where TIdentity : Identity<TIdentity, TKey>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityConverter{TIdentity, TKey}"/> class.
    /// </summary>
    /// <param name="toKey">Expression to convert an <see cref="Identity{TSelf, TKey}"/> into underlying key  value.</param>
    /// <param name="fromKey">Expression to convert underlying key value into an <see cref="Identity{TSelf, TKey}"/>.</param>
    public IdentityConverter(
        Expression<Func<TIdentity, TKey>> toKey,
        Expression<Func<TKey, TIdentity>> fromKey)
    {
        ToKeyExpression = toKey;
        FromKeyExpression = fromKey;
        ToKey = toKey.Compile();
        FromKey = fromKey.Compile();
    }

    /// <summary>
    /// Gets expression for converting Identity to Key.
    /// </summary>
    public Expression<Func<TIdentity, TKey>> ToKeyExpression { get; }

    /// <summary>
    /// Gets expression for converting Key to Identity.
    /// </summary>
    public Expression<Func<TKey, TIdentity>> FromKeyExpression { get; }

    /// <summary>
    /// Gets compiled function for converting Identity to Key.
    /// </summary>
    public Func<TIdentity, TKey> ToKey { get; }

    /// <summary>
    /// Gets compiled function for converting Key to Identity.
    /// </summary>
    public Func<TKey, TIdentity> FromKey { get; }
}

/// <summary>
/// Provides factory methods for creating <see cref="IdentityConverter{TIdentity, TKey}"/> instances.
/// </summary>
public static class IdentityConverter
{
    /// <summary>
    /// Creates an <see cref="IdentityConverter{TIdentity, TKey}"/> for Guid based identity types.
    /// </summary>
    /// <typeparam name="TGuidIdentity">The <see cref="Identity{TSelf, TKey}"/> type.</typeparam>
    /// <param name="fromKey">Expression to convert underlying key value into an <see cref="Identity{TSelf, TKey}"/>.</param>
    /// <returns>An <see cref="IdentityConverter{TIdentity, TKey}"/> for Guid based identity types.</returns>
    public static IdentityConverter<TGuidIdentity, Guid> CreateGuid<TGuidIdentity>(Expression<Func<Guid, TGuidIdentity>> fromKey)
        where TGuidIdentity : Identity<TGuidIdentity, Guid>
    {
        return Create<TGuidIdentity, Guid>(fromKey);
    }

    /// <summary>
    /// Creates an <see cref="IdentityConverter{TIdentity, TKey}"/> for int based identity types.
    /// </summary>
    /// <typeparam name="TIntIdentity">The <see cref="Identity{TSelf, TKey}"/> type.</typeparam>
    /// <param name="fromKey">Expression to convert underlying key value into an <see cref="Identity{TSelf, TKey}"/>.</param>
    /// <returns>An <see cref="IdentityConverter{TIdentity, TKey}"/> for int based identity types.</returns>
    public static IdentityConverter<TIntIdentity, int> CreateInt<TIntIdentity>(Expression<Func<int, TIntIdentity>> fromKey)
        where TIntIdentity : Identity<TIntIdentity, int>
    {
        return Create<TIntIdentity, int>(fromKey);
    }

    /// <summary>
    /// Creates an <see cref="IdentityConverter{TIdentity, TKey}"/> for string based identity types.
    /// </summary>
    /// <typeparam name="TStringIdentity">The <see cref="Identity{TSelf, TKey}"/> type.</typeparam>
    /// <param name="fromKey">Expression to convert underlying key value into an <see cref="Identity{TSelf, TKey}"/>.</param>
    /// <returns>An <see cref="IdentityConverter{TIdentity, TKey}"/> for string based identity types.</returns>
    public static IdentityConverter<TStringIdentity, string> CreateString<TStringIdentity>(Expression<Func<string, TStringIdentity>> fromKey)
        where TStringIdentity : Identity<TStringIdentity, string>
    {
        return Create<TStringIdentity, string>(fromKey);
    }

    private static IdentityConverter<TIdentity, TKey> Create<TIdentity, TKey>(Expression<Func<TKey, TIdentity>> fromKey)
        where TIdentity : Identity<TIdentity, TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(fromKey);

        return new IdentityConverter<TIdentity, TKey>(
            identity => identity.Key,
            fromKey);
    }
}
