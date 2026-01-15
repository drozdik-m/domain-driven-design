using System.Globalization;
using System.Text;

namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Extensions for currency formatting.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Transforms first character of string to upper case.
    /// </summary>
    /// <param name="text">The text to update.</param>
    /// <example>Transforms hello -> Hello.</example>
    /// <returns>The <paramref name="text"/> with first uppercase letter.</returns>
    public static string FirstToUpper(this string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (char.IsUpper(text[0]))
        {
            return text;
        }

        return string.Create(text.Length, text, (span, original) =>
        {
            original.AsSpan().CopyTo(span);
            span[0] = char.ToUpper(span[0], CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Transforms first character of string to upper case.
    /// If the input is null, returns null.
    /// </summary>
    /// <param name="text">The text to update.</param>
    /// <example>Transforms hello -> Hello.</example>
    /// <returns>The <paramref name="text"/> with first uppercase letter.</returns>
    public static string? FirstToUpperOptional(this string? text)
    {
        if (text is null)
        {
            return null;
        }

        return text.FirstToUpper();
    }

    /// <summary>
    /// Removes parts a of string that repeat right next to each other, leaving only one in-place copy.
    /// </summary>
    /// <example>"aaabcaa"(a) => "abca".</example>
    /// <param name="text">The text where duplicates are removed.</param>
    /// <param name="repeatedString">The repeating query.</param>
    /// <returns>String where repeated queries are removed.</returns>
    public static string RemoveNeighbourlyRepeatingString(this string text, string repeatedString)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(repeatedString);

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(repeatedString))
        {
            return text;
        }

        var patternLength = repeatedString.Length;

        // If pattern is longer than text, nothing to remove
        if (patternLength > text.Length)
        {
            return text;
        }

        // Use stack allocation for small results
        if (text.Length <= 512)
        {
            Span<char> buffer = stackalloc char[512];
            var pos = 0;
            var i = 0;

            while (i < text.Length)
            {
                // Check if current position matches the pattern
                if (i + patternLength <= text.Length &&
                    text.AsSpan(i, patternLength).SequenceEqual(repeatedString))
                {
                    // Copy the pattern once
                    repeatedString.AsSpan().CopyTo(buffer[pos..]);
                    pos += patternLength;
                    i += patternLength;

                    // Skip all consecutive repetitions of the same pattern
                    while (i + patternLength <= text.Length &&
                           text.AsSpan(i, patternLength).SequenceEqual(repeatedString))
                    {
                        i += patternLength;
                    }
                }
                else
                {
                    // Not a pattern match, copy single character
                    buffer[pos] = text[i];
                    pos++;
                    i++;
                }
            }

            // If nothing was removed, return original
            if (pos == text.Length)
            {
                return text;
            }

            return new string(buffer[..pos]);
        }

        // For large strings, use StringBuilder
        var sb = new StringBuilder(text.Length);
        var index = 0;

        while (index < text.Length)
        {
            // Check if current position matches the pattern
            if (index + patternLength <= text.Length &&
                text.AsSpan(index, patternLength).SequenceEqual(repeatedString))
            {
                // Append the pattern once
                sb.Append(repeatedString);
                index += patternLength;

                // Skip all consecutive repetitions of the same pattern
                while (index + patternLength <= text.Length &&
                       text.AsSpan(index, patternLength).SequenceEqual(repeatedString))
                {
                    index += patternLength;
                }
            }
            else
            {
                // Not a pattern match, append single character
                sb.Append(text[index]);
                index++;
            }
        }

        var result = sb.ToString();

        // If nothing was removed, return original
        return result.Length == text.Length
            ? text
            : result;
    }
}
