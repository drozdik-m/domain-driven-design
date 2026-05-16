using MartinDrozdik.DDD.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// A DbContext that provides common operations for handling DDD entities and aggregates.
/// </summary>
public class DddDbContext(DbContextOptions options) : DbContext(options)
{
    /// <inheritdoc />
    public override int SaveChanges()
    {
        OnSaveChanges();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnSaveChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnSaveChanges();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Method invoked on any SaveChanges call for all aggregate roots.
    /// </summary>
    /// <param name="entityEntries">The list of entity entries representing aggregate roots.</param>
    protected virtual void OnAggregatesSave(IEnumerable<EntityEntry> entityEntries)
    {
    }

    /// <summary>
    /// Method invoked on any SaveChanges call for all domain entities.
    /// </summary>
    /// <param name="entityEntries">The list of entity entries representing domain entities.</param>
    protected virtual void OnDomainEntitiesSave(IEnumerable<EntityEntry> entityEntries)
    {
    }

    /// <summary>
    /// Method invoked on any SaveChanges call for all entities.
    /// </summary>
    /// <param name="entityEntries">The list of entity entries.</param>
    protected virtual void OnObjectsSave(IEnumerable<EntityEntry> entityEntries)
    {
    }

    /// <summary>
    /// Method invoked on any SaveChanges call.
    /// </summary>
    private void OnSaveChanges()
    {
        var entries = ChangeTracker.Entries().ToArray();
        var aggregateRoots = entries
            .Where(e => e.Entity.GetType().IsAggregateRoot())
            .ToArray();
        var domainEntities = entries
            .Where(e => e.Entity.GetType().IsDomainEntity())
            .ToArray();

        if (aggregateRoots.Length > 0)
        {
            OnAggregatesSave(aggregateRoots);
        }

        if (domainEntities.Length > 0)
        {
            OnDomainEntitiesSave(domainEntities);
        }

        if (entries.Length > 0)
        {
            OnObjectsSave(entries);
        }
    }
}
