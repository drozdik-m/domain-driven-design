using MartinDrozdik.DDD.Blobs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

/// <summary>
/// Maps <see cref="BlobContainer"/> to the column holding it.
/// </summary>
internal static class BlobContainerConversion
{
    /// <summary>
    /// Gets the conversion between a <see cref="BlobContainer"/> and its column.
    /// </summary>
    public static ValueConverter<BlobContainer, string> Converter { get; } = new(
        container => container.Name,
        name => BlobContainer.Create(name).Value);

    /// <summary>
    /// Gets the comparison deciding whether a <see cref="BlobContainer"/> changed.
    /// </summary>
    public static ValueComparer<BlobContainer> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Name == right.Name,
        container => container.Name.GetHashCode(StringComparison.Ordinal),
        container => container);
}
