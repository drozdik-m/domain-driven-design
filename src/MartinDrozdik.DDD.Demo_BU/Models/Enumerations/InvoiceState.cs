namespace MartinDrozdik.DDD.Demo.Models.Enumerations;

public class InvoiceState(EnumerationName name) : Enumeration(name)
{
    public static InvoiceState Draft => new(new EnumerationName("Draft"));

    public static InvoiceState Issued => new(new EnumerationName("Issued"));

    public static InvoiceState Paid => new(new EnumerationName("Paid"));
}
