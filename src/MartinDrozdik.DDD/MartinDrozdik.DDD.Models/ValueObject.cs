using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MartinDrozdik.DDD.Models;

/// <summary>
/// Represents a base class for value objects in a domain-driven design context.
/// Value objects are immutable and are compared based on their properties rather than by <b>identity</b>/reference.
/// </summary>
/// <remarks>
/// Inspired by <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects">.NET Microservices: Architecture for Containerized .NET Applications</a>.
/// </remarks>
public abstract class ValueObject : IEqualityComparer<ValueObject>, IEqualityOperators<ValueObject, ValueObject, bool>
{
    /// <summary>
    /// Compares two <see cref="ValueObject"/>s for equality.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>True if equal, else false.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Compares two <see cref="ValueObject"/>s for equality.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>True if not equal, else false.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);

    /// <summary>
    /// Compares two <see cref="ValueObject"/>s for equality.
    /// </summary>
    /// <param name="obj">The object <i>this</i> object is compared to.</param>
    /// <returns>True if equal, else false.</returns>
    public override bool Equals(object? obj)
        => Equals(this, obj as ValueObject);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    /// <inheritdoc />
    public bool Equals(ValueObject? left, ValueObject? right)
    {
        if (left is null ^ right is null)
        {
            return false;
        }

        return ReferenceEquals(left, right) || left!.GetEqualityComponents().SequenceEqual(right!.GetEqualityComponents());
    }

    /// <inheritdoc />
    public int GetHashCode([DisallowNull] ValueObject obj)
    {
        return obj.GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate(0, (acc, item) => HashCode.Combine(acc, item));
    }

    /// <summary>
    /// List of properties that are used to compare two <see cref="ValueObject"/>s.
    /// </summary>
    /// <returns>List of objects to compare.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
