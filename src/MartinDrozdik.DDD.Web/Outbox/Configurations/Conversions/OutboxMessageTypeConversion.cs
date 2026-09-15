using MartinDrozdik.DDD.Outbox;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Outbox.Configurations.Conversions;

/// <summary>
/// Maps <see cref="OutboxMessageType"/> to the string column that stores it.
/// </summary>
internal static class OutboxMessageTypeConversion
{
    /// <summary>
    /// Gets the converter between <see cref="OutboxMessageType"/> and its key.
    /// </summary>
    public static ValueConverter<OutboxMessageType, string> Converter { get; } = new(
        messageType => messageType.Key,
        key => new OutboxMessageType(key));

    /// <summary>
    /// Gets the comparer used for change tracking.
    /// </summary>
    public static ValueComparer<OutboxMessageType> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Key == right.Key,
        messageType => messageType.Key.GetHashCode(StringComparison.Ordinal),
        messageType => messageType);
}
