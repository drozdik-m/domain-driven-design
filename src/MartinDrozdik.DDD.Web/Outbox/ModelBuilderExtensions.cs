using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Maps the outbox message table into the model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this from the <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of the context that owns your aggregates.
    /// The outbox only works when messages are written by the same transaction as the change that produced them.
    /// No <see cref="DbSet{TEntity}"/> is needed; the engine reaches the table through <see cref="DbContext.Set{TEntity}()"/>.
    /// </para>
    /// <para>
    /// Every context has an outbox and a table of its own.
    /// </para>
    /// </remarks>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to extend.</param>
    /// <param name="table">The table holding the messages.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    ///     modelBuilder.AddOutbox("InvoiceOutbox"); // or new TableName("Outbox", "invoicing")
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder AddOutbox(this ModelBuilder modelBuilder, TableName table)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(table);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(table));

        return modelBuilder;
    }
}
