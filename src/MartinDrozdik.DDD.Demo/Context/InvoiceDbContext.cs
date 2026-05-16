using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Web.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MartinDrozdik.DDD.Demo.Context;

public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options, TimeProvider timeProvider) : DddDbContext(options)
{
    public const string CreatedAtPropertyName = "CreatedAt";
    private const string UpdatedAtPropertyName = "UpdatedAt";

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Person> People => Set<Person>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        // Register audit shadow properties
        foreach (var entityType in modelBuilder.Model.GetAggregateRoots())
        {
            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(CreatedAtPropertyName);

            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(UpdatedAtPropertyName);
        }
    }

    protected override void OnAggregatesSave(IEnumerable<EntityEntry> entityEntries)
    {
        base.OnAggregatesSave(entityEntries);

        var now = timeProvider.GetUtcNow();
        UpdateShadowProperties(entityEntries, now);
    }

    private static void UpdateShadowProperties(IEnumerable<EntityEntry> entityEntries, DateTimeOffset now)
    {
        foreach (var entry in entityEntries.Where(e => e.State == EntityState.Added))
        {
            entry.Property(CreatedAtPropertyName).CurrentValue = now;
            entry.Property(UpdatedAtPropertyName).CurrentValue = now;
        }

        foreach (var entry in entityEntries.Where(e => e.State == EntityState.Modified))
        {
            entry.Property(UpdatedAtPropertyName).CurrentValue = now;
        }
    }
}
