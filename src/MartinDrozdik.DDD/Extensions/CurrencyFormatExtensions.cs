using System.Globalization;

namespace MartinDrozdik.DDD.Extensions;

/// <summary>
/// Extensions for currency formatting.
/// </summary>
public static class CurrencyFormatExtensions
{
    private static readonly NumberFormatInfo s_czkFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2,
    };

    /// <summary>
    /// Converts integer to CZK currency format.
    /// Includes thousands separator, two decimal places and "Kč" suffix.
    /// </summary>
    /// <example>1234 converts to 1 234,00 Kč.</example>
    /// <param name="number">The number to convert.</param>
    /// <returns>Integer as currency-formatted string.</returns>
    public static string ToCzk(this int number)
    {
        // Minus sign: 1 char
        // Digits: worst case 10 chars
        // Spaces: worst case 3 chars
        // Decimal separator and two decimal places: 3 chars
        // Suffix " Kč": 3 chars
        // Total worst case: 20 chars
        Span<char> buffer = stackalloc char[32];

        // Format number with N2 format directly into buffer
        if (!number.TryFormat(buffer, out var written, "N2", s_czkFormat))
        {
            // Fallback for edge cases
            return number.ToString("N2", s_czkFormat) + " Kč";
        }

        // Append " Kč" suffix
        const string suffix = " Kč";
        suffix.AsSpan().CopyTo(buffer[written..]);

        return new string(buffer[.. (written + suffix.Length)]);
    }
}
