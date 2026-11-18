using System.Diagnostics;
using System.Globalization;
using MartinDrozdik.DDD.Blobs.Stores;

namespace MartinDrozdik.DDD.Blobs.Signatures;

/// <summary>
/// The bytes every file of one media type starts with, such as <c>%PDF-</c> for <c>application/pdf</c>.
/// </summary>
/// <remarks>
/// Checked by <see cref="BlobContentCopier"/> when <see cref="BlobWriteOptions.ContentSignatures"/> is set.
/// </remarks>
[DebuggerDisplay("{MediaType}: {Pattern}")]
public sealed class ContentSignature
{
    /// <summary>
    /// The longest pattern, in bytes.
    /// </summary>
    /// <remarks>
    /// The size of the first read of <see cref="BlobContentCopier"/>, so the whole header is checked before anything is written.
    /// </remarks>
    public const int MaxLength = BlobContentCopier.BufferSize;

    /// <summary>
    /// The pattern token that matches any byte.
    /// </summary>
    private const string AnyByte = "??";

    /// <summary>
    /// The expected byte at each position from the start, or null where any byte will do.
    /// </summary>
    private readonly byte?[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSignature"/> class.
    /// </summary>
    /// <param name="mediaType">The media type whose files start with the pattern.</param>
    /// <param name="pattern">
    /// The first bytes as space-separated hexadecimal pairs, with <c>??</c> for a byte that may be anything,
    /// e.g. <c>52 49 46 46 ?? ?? ?? ?? 57 45 42 50</c> for WebP.
    /// A lone <c>??</c> accepts any content of at least one byte, for formats without a signature such as <c>text/plain</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The pattern is empty, contains something other than hexadecimal pairs and <c>??</c>, or is longer than <see cref="MaxLength"/> bytes.
    /// </exception>
    public ContentSignature(MediaType mediaType, string pattern)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        MediaType = mediaType;
        _bytes = Parse(pattern);

        if (_bytes.Length > MaxLength)
        {
            throw new ArgumentException($"A signature covers at most {MaxLength} bytes, but this one covers {_bytes.Length}.", nameof(pattern));
        }

        Pattern = string.Join(' ', _bytes.Select(b => b?.ToString("X2", CultureInfo.InvariantCulture) ?? AnyByte));
    }

    /// <summary>
    /// Gets the signatures of common documents, images, audio, video, archives and fonts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Taken from <see href="https://en.wikipedia.org/wiki/List_of_file_signatures">the list of file signatures on Wikipedia</see>.
    /// </para>
    /// <para>
    /// Executables, scripts, keys, disk images and databases are left out on purpose fo security reasons.
    /// </para>
    /// <para>
    /// Text formats such as <c>text/plain</c>, <c>text/csv</c>, <c>application/json</c> or <c>application/xml</c> have no reliable signature and are not listed, so they are refused.
    /// Add them with a lone <c>??</c>, e.g. <c>[.. ContentSignature.Common, new ContentSignature("text/plain", "??")]</c>.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<ContentSignature> Common { get; } =
    [

        // Documents
        new("application/pdf", "25 50 44 46 2D"),
        new("application/postscript", "25 21 50 53"),
        new("application/rtf", "7B 5C 72 74 66 31"),
        new("application/msword", "D0 CF 11 E0 A1 B1 1A E1"),
        new("application/vnd.ms-excel", "D0 CF 11 E0 A1 B1 1A E1"),
        new("application/vnd.ms-powerpoint", "D0 CF 11 E0 A1 B1 1A E1"),
        new("application/vnd.ms-outlook", "D0 CF 11 E0 A1 B1 1A E1"),
        new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "50 4B 03 04"),
        new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "50 4B 03 04"),
        new("application/vnd.openxmlformats-officedocument.presentationml.presentation", "50 4B 03 04"),
        new("application/vnd.oasis.opendocument.text", "50 4B 03 04"),
        new("application/vnd.oasis.opendocument.spreadsheet", "50 4B 03 04"),
        new("application/vnd.oasis.opendocument.presentation", "50 4B 03 04"),
        new("application/epub+zip", "50 4B 03 04"),
        new("image/vnd.djvu", "41 54 26 54 46 4F 52 4D ?? ?? ?? ?? 44 4A 56"),

        // Images
        new("image/png", "89 50 4E 47 0D 0A 1A 0A"),
        new("image/jpeg", "FF D8 FF"),
        new("image/gif", "47 49 46 38 37 61"),
        new("image/gif", "47 49 46 38 39 61"),
        new("image/webp", "52 49 46 46 ?? ?? ?? ?? 57 45 42 50"),
        new("image/tiff", "49 49 2A 00"),
        new("image/tiff", "4D 4D 00 2A"),
        new("image/tiff", "49 49 2B 00"),
        new("image/tiff", "4D 4D 00 2B"),
        new("image/bmp", "42 4D"),
        new("image/vnd.microsoft.icon", "00 00 01 00"),
        new("image/heic", AtOffset(4, "66 74 79 70 68 65 69 63")),
        new("image/heic", AtOffset(4, "66 74 79 70 6D")),
        new("image/heif", "00 00 00 18 66 74 79 70 68 65 69 63 00 00 00 00"),
        new("image/heif", AtOffset(4, "66 74 79 70 6D")),
        new("image/jxl", "00 00 00 0C 4A 58 4C 20 0D 0A 87 0A"),
        new("image/jxl", "FF 0A"),
        new("image/jp2", "00 00 00 0C 6A 50 20 20 0D 0A 87 0A"),
        new("image/vnd.adobe.photoshop", "38 42 50 53"),

        // Audio
        new("audio/mpeg", "FF FB"),
        new("audio/mpeg", "FF F3"),
        new("audio/mpeg", "FF F2"),
        new("audio/mpeg", "49 44 33"),
        new("audio/wav", "52 49 46 46 ?? ?? ?? ?? 57 41 56 45"),
        new("audio/aiff", "46 4F 52 4D ?? ?? ?? ?? 41 49 46 46"),
        new("audio/flac", "66 4C 61 43"),
        new("audio/ogg", "4F 67 67 53"),
        new("audio/webm", "1A 45 DF A3"),
        new("audio/midi", "4D 54 68 64"),
        new("audio/amr", "23 21 41 4D 52"),
        new("audio/x-ms-wma", "30 26 B2 75 8E 66 CF 11 A6 D9 00 AA 00 62 CE 6C"),

        // Video
        new("video/mp4", AtOffset(4, "66 74 79 70")),
        new("video/3gpp", AtOffset(4, "66 74 79 70 33 67")),
        new("video/3gpp2", AtOffset(4, "66 74 79 70 33 67")),
        new("video/webm", "1A 45 DF A3"),
        new("video/x-matroska", "1A 45 DF A3"),
        new("video/ogg", "4F 67 67 53"),
        new("video/x-msvideo", "52 49 46 46 ?? ?? ?? ?? 41 56 49 20"),
        new("video/mpeg", "00 00 01 BA"),
        new("video/mpeg", "00 00 01 B3"),
        new("video/mp2t", "47"),
        new("video/x-ms-wmv", "30 26 B2 75 8E 66 CF 11 A6 D9 00 AA 00 62 CE 6C"),
        new("video/x-flv", "46 4C 56"),

        // Archives
        new("application/zip", "50 4B 03 04"),
        new("application/zip", "50 4B 05 06"),
        new("application/zip", "50 4B 07 08"),
        new("application/gzip", "1F 8B"),
        new("application/x-bzip2", "42 5A 68"),
        new("application/x-7z-compressed", "37 7A BC AF 27 1C"),
        new("application/vnd.rar", "52 61 72 21 1A 07 00"),
        new("application/vnd.rar", "52 61 72 21 1A 07 01 00"),
        new("application/x-xz", "FD 37 7A 58 5A 00"),
        new("application/zstd", "28 B5 2F FD"),
        new("application/x-tar", AtOffset(257, "75 73 74 61 72 00 30 30")),
        new("application/x-tar", AtOffset(257, "75 73 74 61 72 20 20 00")),
        new("application/vnd.ms-cab-compressed", "4D 53 43 46"),

        // Fonts
        new("font/woff", "77 4F 46 46"),
        new("font/woff2", "77 4F 46 32"),
        new("font/ttf", "00 01 00 00 00"),
        new("font/otf", "4F 54 54 4F"),
    ];

    /// <summary>
    /// Gets the media type whose files start with <see cref="Pattern"/>.
    /// </summary>
    public MediaType MediaType { get; }

    /// <summary>
    /// Gets the first bytes as uppercase hexadecimal pairs, with <c>??</c> for a byte that may be anything.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the number of bytes the pattern covers.
    /// </summary>
    public int Length => _bytes.Length;

    /// <summary>
    /// Decides whether content starts with this signature.
    /// </summary>
    /// <param name="header">The first bytes of the content, at least <see cref="Length"/> of them when the content is that long.</param>
    /// <returns>True when the header starts with the pattern, else false.</returns>
    public bool Matches(ReadOnlySpan<byte> header)
    {
        if (header.Length < _bytes.Length)
        {
            return false;
        }

        for (var i = 0; i < _bytes.Length; i++)
        {
            if (_bytes[i] is { } expected && header[i] != expected)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"{MediaType}: {Pattern}";

    /// <summary>
    /// Prefixes a pattern with wildcards, for a signature that does not start at the first byte.
    /// </summary>
    /// <param name="offset">The number of bytes before the pattern.</param>
    /// <param name="pattern">The pattern found at <paramref name="offset"/>.</param>
    /// <returns>The pattern from the first byte.</returns>
    private static string AtOffset(int offset, string pattern)
        => string.Join(' ', Enumerable.Repeat(AnyByte, offset).Append(pattern));

    /// <summary>
    /// Reads a pattern of hexadecimal pairs and <c>??</c> wildcards.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <returns>The expected byte at each position, or null where any byte will do.</returns>
    /// <exception cref="ArgumentException">The pattern is not valid.</exception>
    private static byte?[] Parse(string pattern)
    {
        var tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new byte?[tokens.Length];

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token == AnyByte)
            {
                continue;
            }

            if (token.Length != 2 || !byte.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            {
                throw new ArgumentException($"A signature is written as hexadecimal pairs and '{AnyByte}' separated by spaces, but '{token}' in '{pattern}' is neither.", nameof(pattern));
            }

            bytes[i] = value;
        }

        return bytes;
    }
}
