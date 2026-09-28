using System.Collections.Frozen;

namespace MartinDrozdik.DDD.Extensions;

/// <summary>
/// Provides extension methods for path manipulation.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// The maximum number of characters of a name returned by <see cref="ToStoredFileName(string)"/>.
    /// </summary>
    /// <remarks>
    /// The lowest common denominator of the file systems worth supporting - ext4, APFS and NTFS all stop at 255.
    /// </remarks>
    public const int MaxStoredFileNameLength = 255;

    /// <summary>
    /// The name <see cref="ToStoredFileName(string)"/> falls back to when sanitization leaves nothing usable.
    /// </summary>
    public const string StoredFileNameFallback = "file";

    /// <summary>
    /// Cross-platform invalid filename characters.
    /// Windows + generally unsafe characters across filesystems.
    /// </summary>
    private static readonly char[] s_crossPlatformInvalidFileNameChars =
    [
        '<', '>', ':', '"', '/', '\\', '|', '?', '*',
    ];

    private static readonly char[] s_invalidFileNameChars = BuildInvalidCharSet();

    /// <summary>
    /// Names Windows reserves for devices. Creating a file under any of them fails, with or without an extension.
    /// </summary>
    private static readonly FrozenSet<string> s_reservedDeviceNames = new[]
    {
        "CON", "PRN", "AUX", "NUL",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a string to a path-friendly format by replacing whitespace with hyphens and removing invalid filename characters.
    /// </summary>
    /// <param name="name">The string to convert.</param>
    /// <returns>A path-friendly name.</returns>
    public static string ToFriendlyFileName(this string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return string.Create(name.Length, (name, s_invalidFileNameChars), (span, state) =>
        {
            state.name.AsSpan().CopyTo(span);

            for (var i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (char.IsWhiteSpace(c) || Array.IndexOf(state.s_invalidFileNameChars, c) >= 0)
                {
                    span[i] = '-';
                }
            }
        });
    }

    /// <summary>
    /// Converts a file name that came from outside the application into one that is safe to create on disk.
    /// </summary>
    /// <remarks>
    /// This is the hardened counterpart of <see cref="ToFriendlyFileName(string)"/>,
    /// meant for names supplied by a user or an HTTP client.
    /// On top of replacing invalid characters it:
    /// <list type="bullet">
    ///     <item>keeps only the last segment, so <c>../../etc/passwd</c> and <c>C:\Windows\system.ini</c> cannot escape a folder,</item>
    ///     <item>trims leading and trailing dots, so <c>.</c>, <c>..</c> and hidden-file names cannot be produced,</item>
    ///     <item>escapes names Windows reserves for devices, such as <c>CON</c> or <c>LPT1</c>,</item>
    ///     <item>caps the result at <see cref="MaxStoredFileNameLength"/> characters, keeping the extension,</item>
    ///     <item>falls back to <see cref="StoredFileNameFallback"/> when nothing usable survives.</item>
    /// </list>
    /// The result is always a non-empty, single path segment.
    /// </remarks>
    /// <param name="name">The file name to sanitize.</param>
    /// <returns>A name that is safe to use as a single path segment.</returns>
    /// <example>
    /// <code>
    /// "../../etc/passwd".ToStoredFileName()      // "passwd"
    /// "Faktura 2026 (final).pdf".ToStoredFileName() // "Faktura-2026-(final).pdf"
    /// "CON.txt".ToStoredFileName()               // "_CON.txt"
    /// "...".ToStoredFileName()                   // "file"
    /// </code>
    /// </example>
    public static string ToStoredFileName(this string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // A browser sends whatever the client called the file, sometimes a whole path.
        // Only the last segment is a name.
        var lastSeparator = name.AsSpan().LastIndexOfAny('/', '\\');
        var candidate = lastSeparator >= 0
            ? name[(lastSeparator + 1)..]
            : name;

        // Friendly name transform
        candidate = candidate.Trim().ToFriendlyFileName();

        // Windows silently drops trailing dots and spaces, which would leave the stored name different from the recorded one.
        // Leading dots are trimmed too, so neither a relative path segment nor a hidden file can be produced.
        candidate = candidate.Trim('.', ' ');

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return StoredFileNameFallback;
        }

        candidate = EscapeReservedDeviceName(candidate);
        candidate = Truncate(candidate);

        // Truncation can uncover a trailing dot
        candidate = candidate.TrimEnd('.', ' ');

        return string.IsNullOrWhiteSpace(candidate)
            ? StoredFileNameFallback
            : candidate;
    }

    /// <summary>
    /// Prefixes a name Windows reserves for a device.
    /// </summary>
    /// <remarks>
    /// The reservation covers the stem, so <c>NUL</c> and <c>NUL.txt</c> are both refused by the operating system.
    /// </remarks>
    /// <param name="name">The sanitized name to inspect.</param>
    /// <returns>The name, prefixed with an underscore when it names a device.</returns>
    private static string EscapeReservedDeviceName(string name)
    {
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        var stem = dot >= 0 ? name[..dot] : name;

        return s_reservedDeviceNames.Contains(stem)
            ? $"_{name}"
            : name;
    }

    /// <summary>
    /// Shortens a name to <see cref="MaxStoredFileNameLength"/> characters, keeping its extension.
    /// </summary>
    /// <param name="name">The name to shorten.</param>
    /// <returns>The name, at most <see cref="MaxStoredFileNameLength"/> characters long.</returns>
    private static string Truncate(string name)
    {
        if (name.Length <= MaxStoredFileNameLength)
        {
            return name;
        }

        var extension = Path.GetExtension(name);

        // An "extension" longer than this is probably something else, just drop it
        if (extension.Length is 0 or > 32)
        {
            return name[..MaxStoredFileNameLength];
        }

        var stemLength = name.Length - extension.Length;
        var keptStemLength = MaxStoredFileNameLength - extension.Length;
        var stem = name[..stemLength];

        return stem[..keptStemLength] + extension;
    }

    /// <summary>
    /// Builds a comprehensive set of invalid filename characters by combining <see cref="s_crossPlatformInvalidFileNameChars"/> and <see cref="Path.GetInvalidFileNameChars()"/>.
    /// </summary>
    private static char[] BuildInvalidCharSet()
    {
        var runtime = Path.GetInvalidFileNameChars();
        var runtimeSet = new HashSet<char>(runtime);

        foreach (var c in s_crossPlatformInvalidFileNameChars)
        {
            runtimeSet.Add(c);
        }

        return [.. runtimeSet];
    }
}
