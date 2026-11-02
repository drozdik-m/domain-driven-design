using System.Globalization;
using System.Text;

namespace MartinDrozdik.DDD.Extensions;

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
    /// Transforms first character of string to lower case.
    /// </summary>
    /// <param name="text">The text to update.</param>
    /// <example>Transforms Hello -> hello.</example>
    /// <returns>The <paramref name="text"/> with first lowercase letter.</returns>
    public static string FirstToLower(this string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (char.IsLower(text[0]))
        {
            return text;
        }

        return string.Create(text.Length, text, (span, original) =>
        {
            original.AsSpan().CopyTo(span);
            span[0] = char.ToLower(span[0], CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Transforms first character of string to lower case.
    /// If the input is null, returns null.
    /// </summary>
    /// <param name="text">The text to update.</param>
    /// <example>Transforms Hello -> hello.</example>
    /// <returns>The <paramref name="text"/> with first lowercase letter.</returns>
    public static string? FirstToLowerOptional(this string? text)
    {
        if (text is null)
        {
            return null;
        }

        return text.FirstToLower();
    }

    /// <summary>
    /// Shortens a text to at most <paramref name="maxLength"/> UTF-16 code units without cutting a character in half.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A plain <c>text[..maxLength]</c> can end in the middle of a character.
    /// A lone high surrogate is not valid UTF-16, and strict UTF-8 encoders refuse it (f.e. PostgreSQL).
    /// </para>
    /// <para>
    /// The result may be shorter than <paramref name="maxLength"/>.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to shorten.</param>
    /// <param name="maxLength">The most UTF-16 code units the result may have, as in <see cref="string.Length"/>.</param>
    /// <returns>The <paramref name="text"/> itself when it fits, else its longest prefix that fits and ends on a character boundary.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    /// <example>
    /// <code>
    /// "Hello".Truncate(3)         // "Hel"
    /// "ab\U0001F600".Truncate(3) // "ab", the emoji takes two code units
    /// "cafe\u0301".Truncate(4)   // "caf", the accent belongs to the "e"
    /// </code>
    /// </example>
    public static string Truncate(this string text, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (maxLength == 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        // TODO more efficient approach (skip to around the end?)
        // Count the appropriate length in UTF-16 code units
        var length = 0;
        while (length < maxLength)
        {
            var next = StringInfo.GetNextTextElementLength(text.AsSpan(length));
            if (length + next > maxLength)
            {
                break;
            }

            length += next;
        }

        // Not even the first text element fits, so at least keep the surrogate pairs whole
        if (length == 0 && maxLength > 0)
        {
            length = char.IsHighSurrogate(text[maxLength - 1])
                ? maxLength - 1
                : maxLength;
        }

        return text[..length];
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
