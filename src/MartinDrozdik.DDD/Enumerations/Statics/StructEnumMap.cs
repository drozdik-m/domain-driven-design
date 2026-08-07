using System.Collections.Frozen;

namespace MartinDrozdik.DDD.Enumerations.Statics;

/// <summary>
/// Caches the map of <see cref="EnumerationName"/>s to the members of a plain .NET <see cref="Enum"/>.
/// </summary>
/// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
internal static class StructEnumMap<TEnum>
    where TEnum : struct, Enum
{
    // A static constructor would be just as lazy, but it wraps the first failure in a TypeInitializationException
    // and reports a NullReferenceException on every access after that. Lazy<T> rethrows the ArgumentException itself.
    // Call EnumerationStructMapping.ThrowIfIncomplete to force the check at startup instead.
    private static readonly Lazy<FrozenDictionary<EnumerationName, TEnum>> s_lazyByName = new(Build);

    /// <summary>
    /// Gets the members of the .NET enum keyed by the <see cref="EnumerationName"/> they map to.
    /// </summary>
    /// <exception cref="ArgumentException">When the enum type cannot be mapped.</exception>
    internal static FrozenDictionary<EnumerationName, TEnum> ByName => s_lazyByName.Value;

    /// <summary>
    /// Builds the map of <see cref="EnumerationName"/>s to the members of the .NET enum.
    /// </summary>
    /// <returns>The members keyed by their <see cref="EnumerationName"/>.</returns>
    /// <exception cref="ArgumentException">When the enum type cannot be mapped.</exception>
    private static FrozenDictionary<EnumerationName, TEnum> Build()
        => StructEnumNames
            .MapFor(typeof(TEnum))
            .ToFrozenDictionary(pair => pair.Value, pair => Enum.Parse<TEnum>(pair.Key));
}
