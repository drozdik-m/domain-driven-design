using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox.Configurations.Conversions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Web.Outbox.Configurations;

/// <summary>
/// Maps <see cref="OutboxMessage"/>.
/// Applied by <see cref="ModelBuilderExtensions.AddOutbox(ModelBuilder, TableName)"/>.
/// </summary>
/// <param name="table">The table holding the messages.</param>
internal sealed class OutboxMessageConfiguration(TableName table)
    : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(table.Name, table.Schema);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
               .ValueGeneratedNever();

        builder.Property(m => m.MessageType)
               .HasConversion(OutboxMessageTypeConversion.Converter, OutboxMessageTypeConversion.Comparer)
               .HasMaxLength(OutboxMessageType.MaxLength)
               .IsRequired();

        // Deliberately unbounded - an optional cap is applied at enqueue time from OutboxOptions
        builder.Property(m => m.Payload)
               .HasConversion(OutboxPayloadConversion.Converter, OutboxPayloadConversion.Comparer)
               .IsRequired();

        builder.Property(m => m.OccurredAt)
               .IsRequired();

        builder.Property(m => m.AvailableAt)
               .IsRequired();

        builder.Property(m => m.ProcessedAt);

        builder.Property(m => m.FailedAt);

        builder.Property(m => m.Attempts)
               .IsRequired();

        builder.Property(m => m.LastError)
               .HasMaxLength(OutboxMessage.LastErrorMaxLength);

        builder.Property(m => m.ClaimedBy);

        builder.Property(m => m.ClaimedUntil);

        builder.Property(m => m.ReservedUntil);

        builder.Property(m => m.ConcurrencyStamp)
               .IsConcurrencyToken()
               .IsRequired();

        // Hot path: the dispatcher looking for deliverable messages, longest waiting first.
        builder.HasIndex(m => new { m.ProcessedAt, m.FailedAt, m.AvailableAt })
               .HasDatabaseName($"IX_{table.Name}_ProcessedAt_FailedAt_AvailableAt");
    }
}
