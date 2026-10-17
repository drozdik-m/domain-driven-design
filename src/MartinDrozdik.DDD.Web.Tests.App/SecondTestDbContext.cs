using System.Text.Json;
using MartinDrozdik.DDD.Web.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// A second bounded context with an outbox of its own, so tests can prove that outboxes stay isolated.
/// </summary>
/// <remarks>
/// It lives in a database of its own.
/// </remarks>
/// <param name="options">Options of the context.</param>
public class SecondTestDbContext(DbContextOptions<SecondTestDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The name of the outbox table, deliberately different from the one of <see cref="TestDbContext"/>.
    /// </summary>
    public const string OutboxTableName = "SecondOutboxMessage";

    /// <summary>
    /// Gets the serializer options of the outbox, deliberately different from the defaults so tests can tell the outboxes apart.
    /// </summary>
    public static JsonSerializerOptions OutboxSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// Derives the path of the database of this context from the one of <see cref="TestDbContext"/>.
    /// </summary>
    /// <param name="firstDatabasePath">The path of the database of <see cref="TestDbContext"/>.</param>
    /// <returns>The path of the database of this context.</returns>
    public static string GetDatabasePath(string firstDatabasePath) => $"{firstDatabasePath}.second";

    /// <summary>
    /// Derives the connection string of this context from the one of <see cref="TestDbContext"/>.
    /// </summary>
    /// <param name="firstConnectionString">The connection string of <see cref="TestDbContext"/>.</param>
    /// <returns>The connection string of this context.</returns>
    public static string GetConnectionString(string firstConnectionString)
    {
        var builder = new SqliteConnectionStringBuilder(firstConnectionString);
        builder.DataSource = GetDatabasePath(builder.DataSource);
        return builder.ToString();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutbox(OutboxTableName);
    }
}
