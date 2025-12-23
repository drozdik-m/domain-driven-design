using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Models.Identities.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Demo.Context.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoice");

        builder.HasKey(i => i.Id)
            .HasName("InvoiceId");
        builder.Property(i => i.Id)
            .HasIdentityConvertor(IdentityConverter.CreateGuid(key => new InvoiceId(key)));

        const string issuerIdShadowProperty = "IssuerId";
        builder.Property<PersonId?>(issuerIdShadowProperty)
            .HasColumnName(issuerIdShadowProperty);
        builder
            .HasOne(i => i.Issuer)
            .WithMany()
            .HasForeignKey(issuerIdShadowProperty)
            .HasConstraintName("FK_Invoice_IssuerId_Person_PersonId")
            .IsRequired();

        const string recipientIdShadowProperty = "RecipientId";
        builder.Property<PersonId>(recipientIdShadowProperty)
            .HasColumnName(recipientIdShadowProperty)
            .IsRequired();
        builder
            .HasOne(i => i.Recipient)
            .WithMany()
            .HasForeignKey(recipientIdShadowProperty)
            .HasConstraintName("FK_Invoice_RecipientId_Person_PersonId");
        
        builder.ComplexProperty(i => i.Number, builder =>
        {
            builder.Property(e => e.Year)
                .HasColumnName("InvoiceYear")
                .IsRequired();

            builder.Property(e => e.Order)
                .HasColumnName("InvoiceOrder")
                .IsRequired();
        });

        builder.Property(i => i.State)
            .HasConversion(
                e => e.Name.Key,
                e => new Models.Enumerations.InvoiceState(e))
            .HasColumnName("State")
            .IsRequired();
    }
}
