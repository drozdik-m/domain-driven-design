using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MartinDrozdik.DDD.Web.Outbox.Configurations.Conversions;

/// <summary>
/// Maps <see cref="OutboxPayload"/> to the string column that stores it.
/// </summary>
internal static class OutboxPayloadConversion
{
    /// <summary>
    /// Gets the converter between <see cref="OutboxPayload"/> and its serialized value.
    /// </summary>
    public static ValueConverter<OutboxPayload, string> Converter { get; } = new(
        payload => payload.Value,
        value => new OutboxPayload(value));

    /// <summary>
    /// Gets the comparer used for change tracking.
    /// </summary>
    public static ValueComparer<OutboxPayload> Comparer { get; } = new(
        (left, right) => left != null && right != null && left.Value == right.Value,
        payload => payload.Value.GetHashCode(StringComparison.Ordinal),
        payload => payload);
}
