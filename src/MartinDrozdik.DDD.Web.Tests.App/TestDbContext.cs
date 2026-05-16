using MartinDrozdik.DDD.Web.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// Just an arbitrary context for testing.
/// </summary>
/// <param name="options"></param>
/// <param name="timeProvider"></param>
public class TestDbContext(DbContextOptions<TestDbContext> options, TimeProvider timeProvider) : DddDbContext(options)
{
    /// <summary>
    /// Name of the CreatedAt shadow column.
    /// </summary>
    public const string CreatedAtPropertyName = "CreatedAt";

    /// <summary>
    /// Name of the UpdatedAt shadow column.
    /// </summary>
    public const string UpdatedAtPropertyName = "UpdatedAt";

    /// <summary>
    /// A table of random entities for testing.
    /// </summary>
    public DbSet<SomeAggregateRoot> SomeEntities => Set<SomeAggregateRoot>();

    /// <summary>
    /// A table of random entities for testing.
    /// </summary>
    public DbSet<SomeDomainEntity> SomeDomainEntities => Set<SomeDomainEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);

        // Register audit shadow properties
        foreach (var entityType in modelBuilder.Model.GetAggregateRoots().Concat(modelBuilder.Model.GetDomainEntities()))
        {
            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(CreatedAtPropertyName);

            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(UpdatedAtPropertyName);
        }
    }

    /// <inheritdoc />
    protected override void OnAggregatesSave(IEnumerable<EntityEntry> entityEntries)
    {
        base.OnAggregatesSave(entityEntries);

        var now = timeProvider.GetUtcNow();
        UpdateShadowProperties(entityEntries, now);
    }

    /// <inheritdoc/>
    protected override void OnDomainEntitiesSave(IEnumerable<EntityEntry> entityEntries)
    {
        base.OnDomainEntitiesSave(entityEntries);

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
