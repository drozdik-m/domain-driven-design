using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Testing.Logging;

/// <summary>
/// A single log entry captured by a <see cref="TestLogger"/>.
/// </summary>
/// <param name="Level">The level the entry was logged at.</param>
/// <param name="Category">The category of the logger that produced the entry, e.g. the full name of the logged type.</param>
/// <param name="EventId">The id of the logged event.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The logged exception, if any.</param>
public sealed record LogEntry(LogLevel Level, string Category, EventId EventId, string Message, Exception? Exception)
{
    /// <summary>
    /// Returns a readable representation of this entry, used when an assertion over the captured entries fails.
    /// </summary>
    /// <returns>The entry as a string.</returns>
    public override string ToString()
    {
        return Exception is null
            ? $"[{Level}] {Category}: {Message}"
            : $"[{Level}] {Category}: {Message} ({Exception.GetType().Name}: {Exception.Message})";
    }
}
