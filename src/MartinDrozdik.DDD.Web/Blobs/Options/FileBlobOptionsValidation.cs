using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Validates <see cref="FileBlobOptions"/>: every container has a folder of its own, and every folder a container.
/// </summary>
/// <remarks>
/// Folders are compared ignoring case (the strictest).
/// </remarks>
/// <param name="registry">The registered containers, or null when blob storage itself was not added.</param>
internal sealed class FileBlobOptionsValidation(BlobContainerRegistry? registry = null) : IValidateOptions<FileBlobOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FileBlobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Every container has a folder
        var folders = new List<(string Container, string Folder)>();
        foreach (var (container, containerOptions) in options.Containers)
        {
            if (string.IsNullOrWhiteSpace(containerOptions.Path))
            {
                failures.Add($"Blob container '{container}' has an empty folder. Without one there is nowhere to put a file.");
                continue;
            }

            folders.Add((container.Name, GetNormalizedFolder(containerOptions.Path)));
        }

        // Isolation: no folder shared, none inside another
        for (var i = 0; i < folders.Count; i++)
        {
            for (var j = i + 1; j < folders.Count; j++)
            {
                if (Overlaps(folders[i].Folder, folders[j].Folder))
                {
                    failures.Add($"Blob containers '{folders[i].Container}' and '{folders[j].Container}' overlap: '{folders[i].Folder}' and '{folders[j].Folder}'. Every container needs a folder of its own, outside any other.");
                }
            }
        }

        if (registry is not null)
        {
            // Every registered container has a folder
            foreach (var container in registry.Containers.Where(c => !options.Containers.ContainsKey(c)))
            {
                failures.Add($"Blob container '{container}' is registered but has no folder in the file store. Give it one with {nameof(FileBlobStoreConfig)}.{nameof(FileBlobStoreConfig.WithContainer)}.");
            }

            // Every folder has a registered container
            foreach (var container in options.Containers.Keys.Where(c => !registry.Contains(c)))
            {
                failures.Add($"Blob container '{container}' has a folder in the file store but is not registered. Register it with {nameof(BlobsConfig)}.{nameof(BlobsConfig.WithContainer)}.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Normalizes a folder to its full path with exactly one trailing separator.
    /// </summary>
    /// <param name="path">The folder as configured.</param>
    /// <returns>The normalized folder.</returns>
    internal static string GetNormalizedFolder(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)) + Path.DirectorySeparatorChar;

    /// <summary>
    /// Decides whether two normalized folders are the same, or one lies inside the other.
    /// </summary>
    /// <param name="first">The first folder.</param>
    /// <param name="second">The second folder.</param>
    /// <returns>True when they overlap, else false.</returns>
    private static bool Overlaps(string first, string second)
        => first.StartsWith(second, StringComparison.OrdinalIgnoreCase)
           || second.StartsWith(first, StringComparison.OrdinalIgnoreCase);
}
