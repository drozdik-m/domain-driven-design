using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Contexts;

/// <summary>
/// Simple integration tests for Entity Framework mappings.
/// Tests actual database queries to validate all configurations work.
/// </summary>
/// <typeparam name="TContext">Concrete <see cref="DbContext"/> type.</typeparam>
public abstract class SqlDbContextIntegrationTests<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Gets all DbSet full entity type names for theory data.
    /// </summary>
    /// <returns>The theory data with entity names.</returns>
    public static TheoryData<string> GetEntityTypeNames()
    {
        var allDbSets = typeof(TContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        var data = new TheoryData<string>();
        foreach (var dbSet in allDbSets)
        {
            var fullTypeName = dbSet.PropertyType.GenericTypeArguments[0].FullName;
            Assert.NotNull(fullTypeName);
            data.Add(fullTypeName);
        }

        return data;
    }

    /// <summary>
    /// Tests each entity mapping by querying through EF's pipeline (not raw SQL),
    /// catching column name mismatches, value converter errors, and shadow property issues
    /// regardless of the underlying database provider.
    /// </summary>
    /// <param name="entityName">The full CLR type name of the entity to test.</param>
    [Theory]
    [MemberData(nameof(GetEntityTypeNames))]
    public void Entity_can_be_queried_from_database(string entityName)
    {
        // Arrange
        using var context = GetContext();

        var entityType = context.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.FullName == entityName);
        Assert.NotNull(entityType);

        // Use EF's own Set<T>() via reflection so the full EF pipeline runs:
        // column mapping, value converters, shadow properties, owned entity splits, etc.
        // Raw SQL would bypass all of this.
        var setMethodInfo = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
            ?? throw new InvalidOperationException("Could not find DbContext.Set<T>() method.");
        var setMethod = setMethodInfo.MakeGenericMethod(entityType.ClrType);

        var queryableInvoke = setMethod.Invoke(context, null)
            ?? throw new InvalidOperationException($"Could not get DbSet for entity type {entityType.ClrType.FullName}.");
        var queryable = queryableInvoke as IQueryable;
        Assert.NotNull(queryable);

        // Pull at most 1 row — we just want EF to compile and execute the query.
        // Cast to non-generic IQueryable so we don't need T at compile time.
        _ = queryable
            .Provider
            .Execute<object>(
                Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.FirstOrDefault),
                    [entityType.ClrType],
                    queryable.Expression));

        // If we get here without exception, mapping is valid.
        Assert.True(true);
    }

    /// <summary>
    /// Tests that there are not pending migrations that need to be applied to the database.
    /// </summary>
    [Fact]
    public void No_pending_migrations()
    {
        using var context = GetContext();
        var pendingMigrations = context.Database.GetPendingMigrations();
        Assert.Empty(pendingMigrations);
    }

    /// <summary>
    /// Tests database connectivity.
    /// </summary>
    [Fact]
    public void Database_can_connect()
    {
        using var context = GetContext();
        Assert.True(context.Database.CanConnect());
    }

    /// <summary>
    /// Tests that the model compiles without errors.
    /// </summary>
    [Fact]
    public void Model_compiles_without_errors()
    {
        using var context = GetContext();
        var exception = Record.Exception(() =>
        {
            var model = context.Model;
            foreach (var entity in model.GetEntityTypes().ToList())
            {
                // Force EF to compile the model for this entity, which will catch mapping errors
                var key = entity.FindPrimaryKey();
                _ = key?.Properties;
            }
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that the context model has entities.
    /// Context without entities is meaningless.
    /// </summary>
    [Fact]
    public void Model_has_entities()
    {
        using var context = GetContext();
        var model = context.Model.GetEntityTypes().ToList();
        Assert.NotEmpty(model);
    }

    /// <summary>
    /// Debug test that verifies the context under test can be instantiated without exceptions.
    /// </summary>
    [Fact]
    public void Can_get_context()
    {
        using var context = GetContext();
        Assert.NotNull(context);
    }

    /// <summary>
    /// Creates and returns a functioning context instance for testing.
    /// </summary>
    /// <returns><see cref="DbContext"/> instance.</returns>
    protected abstract TContext GetContext();
}
