using System.Globalization;
using System.Text;

namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Provides extension methods for url manipulation.
/// </summary>
public static class UrlExtensions
{
    /// <summary>
    /// Creates a URL-friendly filename by applying URL-friendly transformation to both name and extension,
    /// and optionally crops it to a maximum length while preserving the extension.
    /// </summary>
    /// <param name="fileName">The filename to convert.</param>
    /// <param name="maxLength">Maximum allowed length of the resulting filename.</param>
    /// <returns>URL-friendly filename.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the extension is too long to fit within maxLength.
    /// </exception>
    public static string ToUrlFriendlyFileName(this string fileName, int maxLength = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (maxLength < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be at least 2.");
        }

        var lastDotIndex = fileName.LastIndexOf('.');

        // No extension
        if (lastDotIndex < 0)
        {
            var friendly = fileName.ToFriendlyFileName().ToUrlFriendly(maxLength: maxLength);
            return friendly.Length > maxLength
                ? friendly.Substring(0, maxLength)
                : friendly;
        }

        var name = fileName.Substring(0, lastDotIndex);
        var extension = fileName.Substring(lastDotIndex + 1); // no dot

        var friendlyName = name.ToFriendlyFileName().ToUrlFriendly();
        var friendlyExtension = extension.ToFriendlyFileName().ToUrlFriendly();

        // If extension disappeared after normalization, treat as no extension
        if (string.IsNullOrEmpty(friendlyExtension))
        {
            return friendlyName.Length > maxLength
                ? friendlyName.Substring(0, maxLength).Trim('-')
                : friendlyName;
        }

        // Validate extension length
        // Needs: 1 char name + '.' + extension
        if (friendlyExtension.Length > maxLength - 2)
        {
            throw new ArgumentException(
                $"Extension '{friendlyExtension}' is too long to fit within maxLength {maxLength}.",
                nameof(fileName));
        }

        var allowedNameLength = maxLength - friendlyExtension.Length - 1; // 1 for '.'

        if (friendlyName.Length > allowedNameLength)
        {
            friendlyName = friendlyName.Substring(0, allowedNameLength).Trim('-');
        }

        return $"{friendlyName}.{friendlyExtension}";
    }

    /// <summary>
    /// Creates a URL and SEO friendly slug by normalizing text, removing special characters,
    /// and replacing spaces with hyphens.
    /// </summary>
    /// <param name="text">Text to slugify.</param>
    /// <param name="maxLength">Maximum length of slug.</param>
    /// <returns>URL and SEO friendly string.</returns>
    public static string ToUrlFriendly(this string text, int maxLength = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Normalize and convert to lowercase
        var normalized = text
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var result = new StringBuilder(normalized.Length);
        var previousWasDash = false;

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            // Check if the character is a letter or a digit if the character is a
            // international character remap it to an ascii valid character
            if (category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.DecimalDigitNumber)
            {
                if (c < 128)
                {
                    result.Append(c);
                }
                else
                {
                    var mapped = RemapInternationalCharToAscii(c);
                    result.Append(mapped);
                }

                previousWasDash = false;
            }

            // Check if the character is to be replaced by a hyphen but only if the last character wasn't
            else if ((category == UnicodeCategory.SpaceSeparator ||
                      category == UnicodeCategory.ConnectorPunctuation ||
                      category == UnicodeCategory.DashPunctuation ||
                      category == UnicodeCategory.OtherPunctuation ||
                      category == UnicodeCategory.MathSymbol)
                      && !previousWasDash)
            {
                result.Append('-');
                previousWasDash = true;
            }
        }

        // Trim dashes from start and end
        var slug = result.ToString().Trim('-');

        // Remove duplicate dashes
        slug = slug.RemoveNeighbourlyRepeatingString("-");

        // Limit length
        slug = slug.Length <= maxLength
            ? slug
            : slug.Substring(0, maxLength).Trim('-');

        return slug;
    }

    /// <summary>
    /// Maps international characters to their ASCII equivalents for URL-friendly conversion.
    /// </summary>
    private static string RemapInternationalCharToAscii(char c)
    {
        return c switch
        {
            'à' or 'å' or 'á' or 'â' or 'ä' or 'ã' or 'ą' => "a",
            'è' or 'é' or 'ê' or 'ë' or 'ę' => "e",
            'ì' or 'í' or 'î' or 'ï' or 'ı' => "i",
            'ò' or 'ó' or 'ô' or 'õ' or 'ö' or 'ø' or 'ő' or 'ð' => "o",
            'ù' or 'ú' or 'û' or 'ü' or 'ŭ' or 'ů' => "u",
            'ç' or 'ć' or 'č' or 'ĉ' => "c",
            'ż' or 'ź' or 'ž' => "z",
            'ś' or 'ş' or 'š' or 'ŝ' => "s",
            'ñ' or 'ń' => "n",
            'ý' or 'ÿ' => "y",
            'ğ' or 'ĝ' => "g",
            'ř' => "r",
            'ł' => "l",
            'đ' => "d",
            'ĥ' => "h",
            'ĵ' => "j",
            'ß' => "ss",
            'þ' => "th",

            // Default: remove unknown international characters
            _ => string.Empty
        };
    }
}
