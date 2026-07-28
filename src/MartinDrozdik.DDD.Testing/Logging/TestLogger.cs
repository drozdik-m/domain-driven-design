using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Testing.Logging;

/// <summary>
/// A <see cref="ILogger"/> that keeps every log entry in memory so tests can assert on the level, message and exception that were logged.
/// </summary>
/// <remarks>
/// Use <see cref="TestedAppBuilder{TProgram}.WithTestingLogger(out TestLogger)"/> or <see cref="For{TCategory}"/> to hand a typed <see cref="ILogger{TCategoryName}"/>.
/// </remarks>
/// <param name="minimumLevel">Entries below this level are not recorded.</param>
public sealed class TestLogger(LogLevel minimumLevel = LogLevel.Trace) : ILogger, ILoggerProvider
{
    private ImmutableList<LogEntry> _entries = [];

    /// <summary>
    /// Gets every recorded entry, in the order they were logged.
    /// </summary>
    /// <remarks>
    /// The returned list is an immutable snapshot.
    /// </remarks>
    public ImmutableList<LogEntry> Entries => Volatile.Read(ref _entries);

    /// <summary>
    /// Gets the entry that was recorded last.
    /// </summary>
    /// <exception cref="InvalidOperationException">Nothing has been logged yet.</exception>
    public LogEntry Last
    {
        get
        {
            var entries = Entries;
            return entries.Count > 0
                ? entries[^1]
                : throw new InvalidOperationException($"No log entry has been recorded by this {nameof(TestLogger)}.");
        }
    }

    /// <summary>
    /// Creates a typed <see cref="ILogger{TCategoryName}"/> recording into this instance,
    /// for injecting into a class under test.
    /// </summary>
    /// <typeparam name="TCategory">The type the logger logs for.</typeparam>
    /// <returns>A logger recording into this instance.</returns>
    public ILogger<TCategory> For<TCategory>()
    {
        return new CategoryLogger<TCategory>(this);
    }

    /// <summary>
    /// Gets the entries logged at the given <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The level to filter by.</param>
    /// <returns>The matching entries.</returns>
    public ImmutableList<LogEntry> At(LogLevel level)
    {
        return Entries.FindAll(entry => entry.Level == level);
    }

    /// <summary>
    /// Gets the entries logged at the given <paramref name="level"/> or above.
    /// </summary>
    /// <example>Everything at least as severe as <see cref="LogLevel.Warning"/>.</example>
    /// <param name="level">The lowest level to include.</param>
    /// <returns>The matching entries.</returns>
    public ImmutableList<LogEntry> AtLeast(LogLevel level)
    {
        return Entries.FindAll(entry => entry.Level >= level);
    }

    /// <summary>
    /// Gets the entries logged by <typeparamref name="TCategory"/>.
    /// </summary>
    /// <typeparam name="TCategory">The type whose entries are requested.</typeparam>
    /// <returns>The matching entries.</returns>
    public ImmutableList<LogEntry> From<TCategory>()
    {
        return From(typeof(TCategory).FullName ?? typeof(TCategory).Name);
    }

    /// <summary>
    /// Gets the entries whose category starts with <paramref name="categoryPrefix"/>,
    /// e.g. an assembly name, to filter out unrelated framework logging.
    /// </summary>
    /// <param name="categoryPrefix">The category prefix to filter by.</param>
    /// <returns>The matching entries.</returns>
    public ImmutableList<LogEntry> From(string categoryPrefix)
    {
        return Entries.FindAll(entry => entry.Category.StartsWith(categoryPrefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Forgets every recorded entry.
    /// </summary>
    public void Clear()
    {
        Volatile.Write(ref _entries, []);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new CategoryLogger(this, categoryName);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Record(logLevel, string.Empty, eventId, state, exception, formatter);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose, the entries are kept for assertions after the application is gone
    }

    private void Record<TState>(
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var entry = new LogEntry(logLevel, category, eventId, formatter(state, exception), exception);

        // Lock-free append: retries until no other thread has appended in the meantime
        ImmutableInterlocked.Update(ref _entries, static (entries, added) => entries.Add(added), entry);
    }

    /// <summary>
    /// Logger recording into its owner under a fixed category.
    /// </summary>
    /// <param name="owner">The recording instance to log into.</param>
    /// <param name="category">The category of this logger.</param>
    private class CategoryLogger(TestLogger owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => owner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            owner.Record(logLevel, category, eventId, state, exception, formatter);
        }
    }

    /// <summary>
    /// Typed logger recording into its owner, satisfying an <see cref="ILogger{TCategoryName}"/> dependency.
    /// </summary>
    /// <typeparam name="TCategory">The type this logger logs for.</typeparam>
    /// <param name="owner">The recording instance to log into.</param>
    private sealed class CategoryLogger<TCategory>(TestLogger owner)
        : CategoryLogger(owner, typeof(TCategory).FullName ?? typeof(TCategory).Name), ILogger<TCategory>
    {
    }
}
