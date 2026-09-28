using MartinDrozdik.DDD.Web.Blobs.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// The default name of the table holding the blob catalogue.
    /// </summary>
    public const string DefaultTableName = "Blob";

    /// <summary>
    /// Maps the blob catalogue into the model.
    /// </summary>
    /// <remarks>
    /// Call this from the <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of the context that owns your´aggregates.
    /// It has to be the same context to ensure atomicity.
    /// No <see cref="DbSet{TEntity}"/> is needed.
    /// </remarks>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to extend.</param>
    /// <param name="tableName">Name of the table holding the catalogue.</param>
    /// <param name="schema">Schema of the table, or null for the default schema of the provider.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    ///     modelBuilder.AddOutbox();
    ///     modelBuilder.AddBlobs();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder AddBlobs(
        this ModelBuilder modelBuilder,
        string tableName = DefaultTableName,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        modelBuilder.ApplyConfiguration(new BlobConfiguration(tableName, schema));

        return modelBuilder;
    }
}
