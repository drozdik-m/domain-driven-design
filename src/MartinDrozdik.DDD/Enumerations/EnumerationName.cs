using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MartinDrozdik.DDD.Identities;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Enumerations;

/// <summary>
/// Enumeration name acting as a unique identifier for an enumeration value.
/// Must not be empty or whitespace.
/// Case sensitive.
/// </summary>
[DebuggerDisplay("{Key}")]
public readonly struct EnumerationName : IIdentity<string>, IEqualityComparer<EnumerationName>, IEquatable<EnumerationName>, IEqualityOperators<EnumerationName, EnumerationName, bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationName"/> struct.
    /// </summary>
    /// <param name="key">The key of the enumeration.</param>
    public EnumerationName(string key)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        KeyLowercase = key.ToLowerInvariant();
    }

    /// <inheritdoc />
    public string Key { get; init; }

    /// <summary>
    /// Gets lowercase version of the <see cref="Key"/> for case-insensitive comparisons.
    /// </summary>
    public string KeyLowercase { get; init; }

    /// <summary>
    /// Implicitly convert a string to an enumeration name.
    /// </summary>
    /// <param name="name">The name of the enumeration.</param>
    public static implicit operator EnumerationName(string name)
    {
        return new EnumerationName(name);
    }

    /// <summary>
    /// Implicitly convert a nullable string to an enumeration name.
    /// </summary>
    /// <param name="name">The name of the enumeration.</param>
    public static implicit operator EnumerationName?(string? name)
    {
        if (name is null)
        {
            return null;
        }

        return new EnumerationName(name);
    }

    /// <summary>
    /// Compares two <see cref="EnumerationName"/>s for equality.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>True if equal, else false.</returns>
    public static bool operator ==(EnumerationName left, EnumerationName right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="ValueObject"/>s for equality.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>True if not equal, else false.</returns>
    public static bool operator !=(EnumerationName left, EnumerationName right)
        => !(left == right);

    /// <summary>
    /// Compares two <see cref="EnumerationName"/>s for equality.
    /// </summary>
    /// <param name="obj">The object <i>this</i> object is compared to.</param>
    /// <returns>True if equal, else false.</returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is EnumerationName name && Equals(name);

    /// <inheritdoc />
    public bool Equals(EnumerationName left, EnumerationName right)
        => left.Key == right.Key;

    /// <summary>
    /// Compares two <see cref="EnumerationName"/>s for equality.
    /// </summary>
    /// <param name="other">The other name.</param>
    /// <returns>True if equal, else false.</returns>
    public bool Equals(EnumerationName other)
        => Equals(this, other);

    /// <inheritdoc />
    public override int GetHashCode()
        => GetHashCode(this);

    /// <inheritdoc />
    public int GetHashCode([DisallowNull] EnumerationName obj)
        => HashCode.Combine(obj.Key);

    /// <summary>
    /// Returns the name of the enumeration.
    /// </summary>
    /// <returns>The key value as string.</returns>
    public override string ToString()
        => Key;
}
