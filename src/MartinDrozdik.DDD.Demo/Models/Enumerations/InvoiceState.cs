using MartinDrozdik.DDD.Enumerations;

namespace MartinDrozdik.DDD.Demo.Models.Enumerations;

/// <summary>
/// The lifecycle state of an invoice.
/// </summary>
/// <remarks>
/// Members must be <c>public static readonly</c> fields.
/// </remarks>
public class InvoiceState : StaticEnumeration<InvoiceState>
{
    /// <summary>
    /// The invoice has been drafted but not issued yet.
    /// </summary>
    public static readonly InvoiceState Draft = new(new EnumerationName("Draft"));

    /// <summary>
    /// The invoice has been issued to its recipient.
    /// </summary>
    public static readonly InvoiceState Issued = new(new EnumerationName("Issued"));

    /// <summary>
    /// The invoice has been paid.
    /// </summary>
    public static readonly InvoiceState Paid = new(new EnumerationName("Paid"));

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceState"/> class.
    /// </summary>
    /// <param name="name">Enumeration member name.</param>
    private InvoiceState(EnumerationName name)
        : base(name)
    {
    }
}
