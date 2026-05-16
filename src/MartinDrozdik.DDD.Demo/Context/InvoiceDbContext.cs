using System.Reflection.Emit;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Templates;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Context;

/*
public class DddDbContext(DbContextOptions<DddDbContext> options) : DbContext(options)
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
    /// Method invoked on any SaveChanges call.
    /// </summary>
    private void OnSaveChanges()
    {
        var aggregateRoots = ChangeTracker.Entries()
            .Where()
        foreach (var entityType in aggregateRootTypes)
    }

    protected virtual void OnAggregatesSave()
    {

    }

    protected virtual void OnEntitiesSave()
    {

    }

    protected virtual void OnObjectsSave()
    {
    }
}*/


public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Person> People => Set<Person>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    }
}
