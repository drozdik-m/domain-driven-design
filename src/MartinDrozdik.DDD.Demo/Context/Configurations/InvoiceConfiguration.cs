using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Identities.Converters;
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
            .HasColumnName(issuerIdShadowProperty)
            .IsRequired(false);
        builder
            .HasOne(i => i.Issuer)
            .WithMany()
            .HasForeignKey(issuerIdShadowProperty)
            .HasConstraintName("FK_Invoice_IssuerId_Person_PersonId")
            .IsRequired(false);
        builder.Navigation(i => i.Issuer).AutoInclude();

        const string recipientIdShadowProperty = "RecipientId";
        builder.Property<PersonId>(recipientIdShadowProperty)
            .HasColumnName(recipientIdShadowProperty)
            .IsRequired();
        builder
            .HasOne(i => i.Recipient)
            .WithMany()
            .HasForeignKey(recipientIdShadowProperty)
            .HasConstraintName("FK_Invoice_RecipientId_Person_PersonId");
        builder.Navigation(i => i.Recipient).AutoInclude();

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
                e => Models.Enumerations.InvoiceState.FromName(e).Value)
            .HasColumnName("State")
            .IsRequired();
    }
}
