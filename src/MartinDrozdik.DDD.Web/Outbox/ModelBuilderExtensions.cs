using MartinDrozdik.DDD.Web.Outbox.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// The default name of the table holding outbox messages.
    /// </summary>
    public const string DefaultTableName = "OutboxMessage";

    /// <summary>
    /// Maps the outbox message table into the model.
    /// </summary>
    /// <remarks>
    /// Call this from the <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of the context that owns your aggregates.
    /// The outbox only works when messages are written by the same transaction as the change that produced them.
    /// No <see cref="DbSet{TEntity}"/> is needed; the engine reaches the table through <see cref="DbContext.Set{TEntity}()"/>.
    /// </remarks>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to extend.</param>
    /// <param name="tableName">Name of the table holding the messages.</param>
    /// <param name="schema">Schema of the table, or null for the default schema of the provider.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    ///     modelBuilder.AddOutbox();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder AddOutbox(
        this ModelBuilder modelBuilder,
        string tableName = DefaultTableName,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(tableName, schema));

        return modelBuilder;
    }
}
