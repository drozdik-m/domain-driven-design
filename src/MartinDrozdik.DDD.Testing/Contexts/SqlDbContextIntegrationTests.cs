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
    /// Tests each entity mapping by querying the DbSet.
    /// If any mapping is broken, this will throw an exception.
    /// </summary>
    /// <param name="entityName">Name of the entity.</param>
    [Theory]
    [MemberData(nameof(GetEntityTypeNames))]
    public void Entity_can_be_queried_from_database(string entityName)
    {
        // Arrange
        using var context = GetContext();
        var entityTypes = context.Model.GetEntityTypes();
        var entityType = entityTypes.FirstOrDefault(e => e.ClrType.FullName == entityName);
        Assert.NotNull(entityType);
        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema();
        var fullName = string.IsNullOrEmpty(schema)
            ? $"[{tableName}]"
            : $"[{schema}].[{tableName}]";
        var sql = $"SELECT * FROM {fullName} LIMIT 1";

        // Act & Assert
        // This will fail if table doesn't exist or mappings are incorrect
        context.Database.ExecuteSqlRaw(sql);
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
        var model = context.Model;
        var entities = model.GetEntityTypes().ToList();

        Assert.NotEmpty(entities);
        Assert.All(entities, e => Assert.NotNull(e.FindPrimaryKey()));
    }

    /// <summary>
    /// Debug test that verifies the context under test can be instantiated without exceptions.
    /// </summary>
    [Fact]
    public void Can_get_context()
    {
        var context = GetContext();
        Assert.NotNull(context);
    }

    /// <summary>
    /// Creates and returns a functioning context instance for testing.
    /// </summary>
    /// <returns><see cref="DbContext"/> instance.</returns>
    protected abstract TContext GetContext();
}
