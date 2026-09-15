using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Configurations.Conversions;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Web.Outbox.Configurations;

/// <summary>
/// Maps <see cref="OutboxMessage"/>.
/// Applied by <see cref="ModelBuilderExtensions.AddOutbox(ModelBuilder, string, string?)"/>.
/// </summary>
/// <param name="tableName">Name of the table holding the messages.</param>
/// <param name="schema">Schema of the table, or null for the default schema of the provider.</param>
internal sealed class OutboxMessageConfiguration(string tableName, string? schema)
    : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName, schema);

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

        builder.Property(m => m.ConcurrencyStamp)
               .IsConcurrencyToken()
               .IsRequired();

        // Hot path: the dispatcher looking for deliverable messages.
        builder.HasIndex(m => new { m.ProcessedAt, m.FailedAt, m.AvailableAt })
               .HasDatabaseName($"IX_{tableName}_ProcessedAt_FailedAt_AvailableAt");
    }
}
