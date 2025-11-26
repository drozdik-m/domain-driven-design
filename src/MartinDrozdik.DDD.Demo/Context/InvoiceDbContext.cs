using MartinDrozdik.DDD.Demo.Models.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Context;

public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    }
}
