namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="Random"/> class.
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// Generates a random string of specified length using preset characters.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance to use for generation.</param>
    /// <param name="length">The length of the string to generate.</param>
    /// <param name="allowedChars">The set of characters to choose from.</param>
    /// <returns>A random string of the specified length.</returns>
    public static string String(this Random random, int length, ReadOnlySpan<char> allowedChars)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
        }

        if (allowedChars.IsEmpty)
        {
            throw new ArgumentException("Allowed characters cannot be empty.", nameof(allowedChars));
        }

        if (length == 0)
        {
            return string.Empty;
        }

        // Use stack allocation for small strings
        const int stackAllocThreshold = 128;
        if (length <= stackAllocThreshold)
        {
            Span<char> buffer = stackalloc char[stackAllocThreshold];

            for (var i = 0; i < length; i++)
            {
                buffer[i] = allowedChars[random.Next(allowedChars.Length)];
            }

            return new string(buffer[..length]);
        }

        // For large strings, use string.Create
        return string.Create(length, (random, allowedChars.ToString()), (span, state) =>
        {
            var chars = state.Item2.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[state.random.Next(chars.Length)];
            }
        });
    }

    /// <inheritdoc cref="String(Random, int, ReadOnlySpan{char})" />
    public static string String(this Random random, int length, string allowedChars)
    {
        ArgumentNullException.ThrowIfNull(allowedChars);
        return random.String(length, allowedChars.AsSpan());
    }

    /// <summary>
    /// Common character sets for random string generation.
    /// </summary>
    public static class Sets
    {
        /// <summary>Lowercase letters (a-z).</summary>
        public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>Uppercase letters (A-Z).</summary>
        public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>Digits (0-9).</summary>
        public const string Digits = "0123456789";

        /// <summary>Alphanumeric characters (a-z, A-Z, 0-9).</summary>
        public const string Alphanumeric = Lowercase + Uppercase + Digits;

        /// <summary>Letters only (a-z, A-Z).</summary>
        public const string Letters = Lowercase + Uppercase;

        /// <summary>Hexadecimal characters (0-9, a-f).</summary>
        public const string Hex = "0123456789abcdef";

        /// <summary>URL-safe characters (a-z, A-Z, 0-9, -, _).</summary>
        public const string UrlSafe = Alphanumeric + "-_";

        /// <summary>Common special characters.</summary>
        public const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        /// <summary>Alphanumeric with special characters.</summary>
        public const string AlphanumericSpecial = Alphanumeric + SpecialChars;
    }
}
