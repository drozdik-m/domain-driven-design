using MartinDrozdik.DDD.Blobs.Checksums;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;

/// <summary>
/// Maps <see cref="ChecksumAlgorithm"/> to the column holding its name.
/// </summary>
internal static class ChecksumAlgorithmConversion
{
    /// <summary>
    /// Gets the conversion between a <see cref="ChecksumAlgorithm"/> and its column.
    /// </summary>
    public static ValueConverter<ChecksumAlgorithm, string> Converter { get; } = new(
        algorithm => algorithm.Name.Key,
        value => ChecksumAlgorithm.FromName(value).Value);

    /// <summary>
    /// Gets the comparison deciding whether a <see cref="ChecksumAlgorithm"/> changed.
    /// </summary>
    public static ValueComparer<ChecksumAlgorithm> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Name == right.Name,
        algorithm => algorithm.Name.GetHashCode(),
        algorithm => algorithm);
}
