using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.Tests.Contexts;

/// <summary>
/// Simple integration tests for Entity Framework mappings.
/// Tests actual database queries to validate all configurations work.
/// </summary>
/// <typeparam name="TContext">Concrete <see cref="DbContext"/> type.</typeparam>
public abstract class DbContextIntegrationTests<TContext>
    where TContext
    : DbContext
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
        using var disposeContext = GetContext(out var context);
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

    /*/// <summary>
    /// Tests navigation property by including it in query
    /// Validates foreign keys and relationships are properly configured
    /// </summary>
    [Theory]
    [MemberData(nameof(GetEntityTableNames))]
    public void NavigationProperty_CanBeIncludedInQuery(string entityName, string navigationName)
    {
        // Arrange
        using var disposeContext = GetContext(out var context);

        var entityType = context.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == entityName);

        if (entityType == null) return;

        var dbSetProp = context.GetType()
            .GetProperties()
            .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                 p.PropertyType.GetGenericArguments()[0] == entityType.ClrType);

        if (dbSetProp == null) return;

        var dbSet = dbSetProp.GetValue(context) as IQueryable<object>;
        var query = dbSet.Take(1);

        // Try to include the navigation property
        query = EntityFrameworkQueryableExtensions.Include(query, navigationName);
        query.ToList(); // Execute query

        Assert.True(true);
    }*/

    /// <summary>
    /// Tests that the database schema matches the model.
    /// </summary>
    [Fact]
    public void Database_schema_matches_the_model()
    {
        using var disposeContext = GetContext(out var context);
        var pendingMigrations = context.Database.GetPendingMigrations();
        Assert.Empty(pendingMigrations);
    }

    /// <summary>
    /// Tests database connectivity.
    /// </summary>
    [Fact]
    public void Database_can_connect()
    {
        using var disposeContext = GetContext(out var context);
        Assert.True(context.Database.CanConnect());
    }

    /// <summary>
    /// Tests that the model compiles without errors.
    /// </summary>
    [Fact]
    public void Model_compiles_without_errors()
    {
        using var disposeContext = GetContext(out var context);
        var model = context.Model;
        var entities = model.GetEntityTypes().ToList();

        Assert.NotEmpty(entities);
        Assert.All(entities, e => Assert.NotNull(e.FindPrimaryKey()));
    }

    protected abstract IDisposable GetContext(out TContext context);
}
