namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Provides extension methods for path manipulation.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// Cross-platform invalid filename characters.
    /// Windows + generally unsafe characters across filesystems.
    /// </summary>
    private static readonly char[] s_crossPlatformInvalidFileNameChars =
    {
        '<', '>', ':', '"', '/', '\\', '|', '?', '*',
    };

    private static readonly char[] s_invalidFileNameChars = BuildInvalidCharSet();

    /// <summary>
    /// Converts a string to a path-friendly format by replacing whitespace with hyphens
    /// and removing invalid filename characters.
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
