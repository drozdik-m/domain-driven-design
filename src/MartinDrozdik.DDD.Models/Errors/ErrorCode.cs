using System.Diagnostics;
using MartinDrozdik.DDD.Models.Templates;

namespace MartinDrozdik.DDD.Models.Errors;

/// <summary>
/// An error code that represents a specific identifier of an error type in the system.
/// </summary>
[DebuggerDisplay("{Key}")]
public sealed class ErrorCode : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorCode"/> class.
    /// Validates that the key is not null or whitespace.
    /// </summary>
    /// <param name="key">The error code as string.</param>
    public ErrorCode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
    }

    /// <summary>
    /// Gets code of the error. Each error should have a unique code.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// An implicit casting from string to <see cref="ErrorCode"/>.
    /// </summary>
    /// <param name="code">The error code as string.</param>
    public static implicit operator ErrorCode(string code) => new(code);

    /// <inheritdoc />
    public override string ToString()
    {
        return Key;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }
}
