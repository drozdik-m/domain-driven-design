using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MartinDrozdik.DDD.Models.Tests;

/// <summary>
/// Utility class for testing equality and hash code behavior of types implementing <see cref="IEqualityComparer{T}"/> and <see cref="IEquatable{T}"/>.
/// </summary>
public static class EqualityAssertions
{
    /// <summary>
    /// Tests all equality-related behaviors for a type implementing <see cref="IEquatable{T}"/>, 
    /// <see cref="IEqualityComparer{T}"/>, and <see cref="IEqualityOperators{T, T, TResult}"/>.
    /// </summary>
    /// <typeparam name="T">The type being tested.</typeparam>
    /// <param name="value1">An instance of the type.</param>
    /// <param name="value2">Another instance of the type that is equal to <paramref name="value1"/>.</param>
    /// <param name="differentValue">An instance of the type that is not equal to <paramref name="value1"/>.</param>
    /// <param name="comparer">An instance of the comparer (optional, defaults to <paramref name="value1"/> if it implements <see cref="IEqualityComparer{T}"/>).</param>
    public static void TestAllEqualityBehaviors<T>(
        T value1,
        T value2,
        T differentValue,
        IEqualityComparer<T>? comparer = null)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
    {
        // Use the provided comparer or default to value1 if it implements IEqualityComparer<T>
        comparer ??= value1 as IEqualityComparer<T>
            ?? throw new ArgumentException($"A valid {nameof(IEqualityComparer<T>)} instance must be provided or {nameof(value1)} must implement {nameof(IEqualityComparer<T>)}.");

        // Test equality and hash code behavior
        TestEquatable(value1, value2, differentValue);
        TestEqualityComparer(comparer, value1, value2, differentValue);
        TestEqualityOperators(value1, value2, differentValue);
    }

    /// <summary>
    /// Tests equality and hash code behavior for a type implementing <see cref="IEquatable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being tested.</typeparam>
    /// <param name="value1">An instance of the type.</param>
    /// <param name="value2">Another instance of the type that is equal to <paramref name="value1"/>.</param>
    /// <param name="differentValue">An instance of the type that is not equal to <paramref name="value1"/>.</param>
    public static void TestEquatable<T>(T value1, T value2, T differentValue)
        where T : IEquatable<T>
    {
        // Test equality using Equals
        Assert.True(value1.Equals(value2), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should return true for equal values.");
        Assert.True(value2.Equals(value1), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should be symmetric.");
        Assert.False(value1.Equals(differentValue), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should return false for different values.");
        Assert.False(value2.Equals(differentValue), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should return false for different values.");

        // Test inequality with null
        Assert.False(value1.Equals(default), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should return false when compared to null.");
        Assert.False(value2.Equals(default), $"{nameof(IEquatable<T>)}.{nameof(IEquatable<T>.Equals)} should return false when compared to null.");
    }

    /// <summary>
    /// Tests equality and hash code behavior for a type implementing <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being tested.</typeparam>
    /// <param name="comparer">An instance of the comparer.</param>
    /// <param name="value1">An instance of the type.</param>
    /// <param name="value2">Another instance of the type that is equal to <paramref name="value1"/>.</param>
    /// <param name="differentValue">An instance of the type that is not equal to <paramref name="value1"/>.</param>
    public static void TestEqualityComparer<T>(IEqualityComparer<T> comparer, [DisallowNull] T value1, [DisallowNull] T value2, [DisallowNull] T differentValue)
    {
        // Test equality using IEqualityComparer
        Assert.True(comparer.Equals(value1, value2), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should return true for equal values.");
        Assert.True(comparer.Equals(value2, value1), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should be symmetric.");
        Assert.False(comparer.Equals(value1, differentValue), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should return false for different values.");
        Assert.False(comparer.Equals(value2, differentValue), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should return false for different values.");

        // Test inequality with default
        Assert.False(comparer.Equals(value1, default), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should return false when compared to default.");
        Assert.False(comparer.Equals(default, value1), $"{nameof(IEqualityComparer<T>)}.{nameof(IEqualityComparer<T>.Equals)} should return false when compared to default.");

        // Test hash codes
        Assert.Equal(comparer.GetHashCode(value1), comparer.GetHashCode(value2));
        Assert.NotEqual(comparer.GetHashCode(value1), comparer.GetHashCode(differentValue));
    }

    /// <summary>
    /// Tests equality operators for a type implementing <see cref="IEqualityOperators{T, T, TResult}"/>.
    /// </summary>
    /// <typeparam name="T">The type being tested.</typeparam>
    /// <param name="value1">An instance of the type.</param>
    /// <param name="value2">Another instance of the type that is equal to <paramref name="value1"/>.</param>
    /// <param name="differentValue">An instance of the type that is not equal to <paramref name="value1"/>.</param>
    public static void TestEqualityOperators<T>(T value1, T value2, T differentValue)
        where T : IEqualityOperators<T, T, bool>
    {
        // Test equality operator
        Assert.True(value1 == value2, $"{nameof(IEqualityOperators<T, T, bool>)}.operator == should return true for equal values.");
        Assert.False(value1 != value2, $"{nameof(IEqualityOperators<T, T, bool>)}.operator != should return false for equal values.");

        // Test inequality operator
        Assert.False(value1 == differentValue, $"{nameof(IEqualityOperators<T, T, bool>)}.operator == should return false for different values.");
        Assert.True(value1 != differentValue, $"{nameof(IEqualityOperators<T, T, bool>)}.operator != should return true for different values.");

        // Test null equality
        Assert.False(value1 == default, $"{nameof(IEqualityOperators<T, T, bool>)}.operator == should return false when compared to null.");
        Assert.True(value1 != default, $"{nameof(IEqualityOperators<T, T, bool>)}.operator != should return true when compared to null.");
        Assert.True(default != value1, $"{nameof(IEqualityOperators<T, T, bool>)}.operator != should return true when null is compared to a value.");
        Assert.False(default == value1, $"{nameof(IEqualityOperators<T, T, bool>)}.operator == should return false when null is compared to a value.");
    }
}
