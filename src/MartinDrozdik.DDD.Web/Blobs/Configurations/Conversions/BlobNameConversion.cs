using MartinDrozdik.DDD.Blobs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

/// <summary>
/// Maps <see cref="BlobName"/> to the column holding it.
/// </summary>
internal static class BlobNameConversion
{
    /// <summary>
    /// Gets the conversion between a <see cref="BlobName"/> and its column.
    /// </summary>
    public static ValueConverter<BlobName, string> Converter { get; } = new(
        name => name.Value,
        value => BlobName.FromStored(value));

    /// <summary>
    /// Gets the comparison deciding whether a <see cref="BlobName"/> changed.
    /// </summary>
    public static ValueComparer<BlobName> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Value == right.Value,
        name => name.Value.GetHashCode(StringComparison.Ordinal),
        name => name);
}
