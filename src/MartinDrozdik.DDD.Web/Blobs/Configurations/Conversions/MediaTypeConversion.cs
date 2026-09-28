using MartinDrozdik.DDD.Blobs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

/// <summary>
/// Maps <see cref="MediaType"/> to the column holding it.
/// </summary>
internal static class MediaTypeConversion
{
    /// <summary>
    /// Gets the conversion between a <see cref="MediaType"/> and its column.
    /// </summary>
    public static ValueConverter<MediaType, string> Converter { get; } = new(
        mediaType => mediaType.Value,
        value => MediaType.Create(value).Value);

    /// <summary>
    /// Gets the comparison deciding whether a <see cref="MediaType"/> changed.
    /// </summary>
    public static ValueComparer<MediaType> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Value == right.Value,
        mediaType => mediaType.Value.GetHashCode(StringComparison.Ordinal),
        mediaType => mediaType);
}
