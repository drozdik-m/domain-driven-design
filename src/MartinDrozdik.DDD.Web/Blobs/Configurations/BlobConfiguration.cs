using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Identities.Converters;
using MartinDrozdik.DDD.Web.Blobs.Configurations.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Web.Blobs.Configurations;

/// <summary>
/// Maps <see cref="Blob"/>.
/// Applied by <see cref="ModelBuilderExtensions.AddBlobs(ModelBuilder, string, string?)"/>.
/// </summary>
/// <param name="tableName">Name of the table holding the catalogue.</param>
/// <param name="schema">Schema of the table, or null for the default schema of the provider.</param>
internal sealed class BlobConfiguration(string tableName, string? schema)
    : IEntityTypeConfiguration<Blob>
{
    private static readonly IdentityConverter<BlobId, Guid> s_idConverter = IdentityConverter.CreateGuid(key => new BlobId(key));

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Blob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName, schema);

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
               .HasConversion(s_idConverter.ToKeyExpression, s_idConverter.FromKeyExpression)
               .ValueGeneratedNever();

        builder.Property(b => b.Container)
               .HasConversion(BlobContainerConversion.Converter, BlobContainerConversion.Comparer)
               .HasMaxLength(BlobContainer.MaxLength)
               .IsRequired();

        builder.Property(b => b.Name)
               .HasConversion(BlobNameConversion.Converter, BlobNameConversion.Comparer)
               .HasMaxLength(BlobName.MaxLength)
               .IsRequired();

        builder.Property(b => b.OriginalFileName)
               .HasMaxLength(Blob.OriginalFileNameMaxLength)
               .IsRequired();

        // Optional - a file that arrived without an extension has none to record
        builder.Property(b => b.Extension)
               .HasMaxLength(Blob.ExtensionMaxLength);

        builder.Property(b => b.ContentType)
               .HasConversion(MediaTypeConversion.Converter, MediaTypeConversion.Comparer)
               .HasMaxLength(MediaType.MaxLength)
               .IsRequired();

        builder.Property(b => b.Size)
               .IsRequired();

        // Optional - both columns are empty whenever hashing was turned off
        builder.ComplexProperty(b => b.Checksum, checksum =>
        {
            checksum.Property(c => c.Algorithm)
                    .HasConversion(ChecksumAlgorithmConversion.Converter, ChecksumAlgorithmConversion.Comparer)
                    .HasColumnName($"{nameof(Blob.Checksum)}{nameof(BlobChecksum.Algorithm)}")
                    .HasMaxLength(ChecksumAlgorithm.NameMaxLength)
                    .IsRequired();

            checksum.Property(c => c.Value)
                    .HasColumnName(nameof(Blob.Checksum))
                    .HasMaxLength(BlobChecksum.MaxLength)
                    .IsRequired();
        });

        // Deliberately unbounded - the limits live on the value object, which enforces them on every edit
        builder.Property(b => b.Metadata)
               .HasConversion(BlobMetadataConversion.Converter, BlobMetadataConversion.Comparer)
               .IsRequired();

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.Property(b => b.CreatedBy)
               .HasMaxLength(Blob.CreatedByMaxLength);

        builder.Property(b => b.ExpiresAt);

        builder.Property(b => b.ConcurrencyStamp)
               .IsConcurrencyToken()
               .IsRequired();

        // The expiry half of the sweep.
        builder.HasIndex(b => b.ExpiresAt)
               .HasDatabaseName($"IX_{tableName}_ExpiresAt");
    }
}
