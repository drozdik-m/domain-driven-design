using MartinDrozdik.DDD.Web.Blobs.Configurations;
using MartinDrozdik.DDD.Web.Databases;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// The name of the table holding the blob catalogue when none is given.
    /// </summary>
    public const string DefaultTableName = "Blob";

    /// <summary>
    /// Maps the blob catalogue into the model, as the <see cref="DefaultTableName"/> table in the default schema of the provider.
    /// </summary>
    /// <remarks>
    /// Call this from the <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of the context that owns your aggregates.
    /// It has to be the same context to ensure atomicity.
    /// No <see cref="DbSet{TEntity}"/> is needed.
    /// </remarks>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to extend.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    ///     modelBuilder.AddOutbox("InvoiceOutbox");
    ///     modelBuilder.AddBlobs();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder AddBlobs(this ModelBuilder modelBuilder)
        => modelBuilder.AddBlobs(DefaultTableName);

    /// <summary>
    /// Maps the blob catalogue into the model.
    /// </summary>
    /// <remarks>
    /// Call this from the <see cref="DbContext.OnModelCreating(ModelBuilder)"/> of the context that owns your aggregates.
    /// It has to be the same context to ensure atomicity.
    /// No <see cref="DbSet{TEntity}"/> is needed.
    /// </remarks>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to extend.</param>
    /// <param name="table">The table holding the catalogue.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.AddBlobs(new TableName("Blob", "files"));
    /// </code>
    /// </example>
    public static ModelBuilder AddBlobs(this ModelBuilder modelBuilder, TableName table)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(table);

        modelBuilder.ApplyConfiguration(new BlobConfiguration(table));

        return modelBuilder;
    }
}
