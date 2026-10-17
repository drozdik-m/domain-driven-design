using System.Diagnostics;
using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// The name of a database table, optionally qualified by its schema.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class TableName : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableName"/> class, in the default schema of the provider.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    /// <exception cref="ArgumentException">The name is null, empty or whitespace.</exception>
    public TableName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableName"/> class, in the given schema.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <exception cref="ArgumentException">The name or the schema is null, empty or whitespace.</exception>
    public TableName(string name, string schema)
        : this(name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        Schema = schema;
    }

    /// <summary>
    /// Gets the name of the table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the schema of the table, or null for the default schema of the provider.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// An implicit casting from <see cref="string"/> to a <see cref="TableName"/> in the default schema of the provider.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    public static implicit operator TableName(string name) => new(name);

    /// <inheritdoc />
    public override string ToString() => Schema is null ? Name : $"{Schema}.{Name}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Schema;
    }
}
