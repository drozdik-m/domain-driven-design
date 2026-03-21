using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// Just an arbitrary context for testing.
/// </summary>
/// <param name="options"></param>
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    /// <summary>
    /// A table of random entities for testing.
    /// </summary>
    public DbSet<SomeEntity> SomeEntities => Set<SomeEntity>();
}
