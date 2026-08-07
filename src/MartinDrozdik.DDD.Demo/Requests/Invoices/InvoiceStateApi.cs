using System.Text.Json.Serialization;
using MartinDrozdik.DDD.Demo.Models.Enumerations;

namespace MartinDrozdik.DDD.Demo.Requests.Invoices;

/// <summary>
/// The API contract counterpart of <see cref="InvoiceState"/>.
/// </summary>
/// <remarks>
/// Members are matched to <see cref="InvoiceState"/> by name.
/// The converter keeps the wire format a string, which is what the enumeration name is.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<InvoiceStateApi>))]
public enum InvoiceStateApi
{
    /// <summary>
    /// The invoice has been drafted but not issued yet.
    /// </summary>
    Draft,

    /// <summary>
    /// The invoice has been issued to its recipient.
    /// </summary>
    Issued,

    /// <summary>
    /// The invoice has been paid.
    /// </summary>
    Paid,
}
