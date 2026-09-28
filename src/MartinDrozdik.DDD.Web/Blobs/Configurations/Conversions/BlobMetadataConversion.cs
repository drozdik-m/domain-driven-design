using System.Text.Json;
using MartinDrozdik.DDD.Blobs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

/// <summary>
/// Maps <see cref="BlobMetadata"/> to the single JSON column holding it.
/// </summary>
/// <remarks>
/// <para>
/// A converter rather than an owned entity or a complex property, because the set of keys is decided by the
/// application at run time and there is no fixed shape to map to named columns.
/// </para>
/// <para>
/// <b>The comparison is the part that matters.</b> Every other converter in this module wraps a single string,
/// so comparing that string is enough. This one wraps a dictionary: Entity Framework Core decides whether to
/// write a row by comparing the current value against a snapshot, and a comparison that only checked
/// references would report an edited dictionary as unchanged and silently drop it. The equality below walks
/// the entries; the hash is taken over them in key order so it agrees with that.
/// </para>
/// <para>
/// The snapshot may still hand back the same instance, because <see cref="BlobMetadata"/> is immutable -
/// <c>With</c> and <c>Without</c> return a new one. If that ever stops being true, this has to become a copy.
/// </para>
/// </remarks>
internal static class BlobMetadataConversion
{
    private static readonly JsonSerializerOptions s_serializerOptions = JsonSerializerOptions.Web;

    /// <summary>
    /// Gets the conversion between <see cref="BlobMetadata"/> and its column.
    /// </summary>
    public static ValueConverter<BlobMetadata, string> Converter { get; } = new(
        metadata => JsonSerializer.Serialize(metadata.Values, s_serializerOptions),
        json => Deserialize(json));

    /// <summary>
    /// Gets the comparison deciding whether <see cref="BlobMetadata"/> changed.
    /// </summary>
    public static ValueComparer<BlobMetadata> Comparer { get; } = new(
        (left, right) => AreEqual(left, right),
        metadata => GetHashCode(metadata),
        metadata => metadata);

    /// <summary>
    /// Reads metadata back from its column.
    /// </summary>
    /// <param name="json">The stored JSON.</param>
    /// <returns>The metadata.</returns>
    private static BlobMetadata Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return BlobMetadata.Empty;
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json, s_serializerOptions);

        return values is null ? BlobMetadata.Empty : BlobMetadata.Create(values).Value;
    }

    /// <summary>
    /// Decides whether two sets of metadata hold the same entries.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>True when they hold the same entries, else false.</returns>
    private static bool AreEqual(BlobMetadata? left, BlobMetadata? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Values.Count != right.Values.Count)
        {
            return false;
        }

        return OrderedEntries(left)
            .Zip(OrderedEntries(right))
            .All(pair => string.Equals(pair.First.Key, pair.Second.Key, StringComparison.Ordinal)
                && string.Equals(pair.First.Value, pair.Second.Value, StringComparison.Ordinal));
    }

    /// <summary>
    /// Hashes metadata over its entries, in key order so the hash does not depend on insertion order.
    /// </summary>
    /// <param name="metadata">The metadata to hash.</param>
    /// <returns>The hash.</returns>
    private static int GetHashCode(BlobMetadata metadata)
    {
        var hash = default(HashCode);

        foreach (var entry in OrderedEntries(metadata))
        {
            hash.Add(entry.Key, StringComparer.Ordinal);
            hash.Add(entry.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Lists the entries in key order.
    /// </summary>
    /// <param name="metadata">The metadata to list.</param>
    /// <returns>The entries, ordered by key.</returns>
    private static IEnumerable<KeyValuePair<string, string>> OrderedEntries(BlobMetadata metadata)
        => metadata.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal);
}
