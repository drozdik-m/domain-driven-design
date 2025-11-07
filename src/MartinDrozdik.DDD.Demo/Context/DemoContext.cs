using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Demo.Context;

public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    }
}


public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoice");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Issuer)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new PersonId(v.Value) : null)
            .HasColumnName("IssuerId");
        builder.Property(i => i.Recipient)
            .HasConversion(
                v => v.Value,
                v => new PersonId(v))
            .HasColumnName("RecipientId")
            .IsRequired();
        builder.Property(i => i.Number)
            .HasConversion(
                v => v.Value,
                v => new InvoiceNumber(v))
            .HasColumnName("Number")
            .IsRequired();
        builder.Property(i => i.State)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<InvoiceState>(v))
            .HasColumnName("State")
            .IsRequired();
    }
}
