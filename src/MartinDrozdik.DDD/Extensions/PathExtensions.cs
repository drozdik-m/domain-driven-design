namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Provides extension methods for path manipulation.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// Converts a string to a path-friendly format by replacing whitespace with hyphens
    /// and removing invalid filename characters.
    /// </summary>
    /// <param name="name">The string to convert.</param>
    /// <returns>A path-friendly name.</returns>
    public static string ToFriendlyFileName(this string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Create(name.Length, (name, invalidChars), (span, state) =>
        {
            state.name.AsSpan().CopyTo(span);

            for (var i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (char.IsWhiteSpace(c) || Array.IndexOf(state.invalidChars, c) >= 0)
                {
                    span[i] = '-';
                }
            }
        });
    }
}
