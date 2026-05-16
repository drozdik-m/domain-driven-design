using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

public class DddDbContextAuditTests(ITestOutputHelper testOutputHelper)
{
    private static readonly DateTimeOffset s_initialTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatedAt_and_UpdatedAt_are_set_to_current_time_when_aggregate_root_is_added()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .Build();
        var context = factory.GetScopedService<TestDbContext>();

        // Act
        context.SomeEntities.Add(new SomeAggregateRoot());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var entity = context.SomeEntities.Local.Single();
        Assert.Equal(s_initialTime, GetCreatedAt(context, entity));
        Assert.Equal(s_initialTime, GetUpdatedAt(context, entity));
    }

    [Fact]
    public async Task CreatedAt_and_UpdatedAt_are_set_to_current_time_when_domain_entity_is_added()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .Build();
        var context = factory.GetScopedService<TestDbContext>();

        // Act
        context.SomeDomainEntities.Add(new SomeDomainEntity());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var entity = context.SomeDomainEntities.Local.Single();
        Assert.Equal(s_initialTime, GetCreatedAt(context, entity));
        Assert.Equal(s_initialTime, GetUpdatedAt(context, entity));
    }

    [Fact]
    public async Task UpdatedAt_reflects_update_time_and_CreatedAt_is_unchanged_for_aggregate_root()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .Build();
        var context = factory.GetScopedService<TestDbContext>();
        var updateTime = s_initialTime.AddHours(3);

        var entity = new SomeAggregateRoot();
        context.SomeEntities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.SetUtcNow(updateTime);
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(s_initialTime, GetCreatedAt(context, entity));
        Assert.Equal(updateTime, GetUpdatedAt(context, entity));
    }

    [Fact]
    public async Task UpdatedAt_reflects_update_time_and_CreatedAt_is_unchanged_for_domain_entity()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .Build();
        var context = factory.GetScopedService<TestDbContext>();
        var updateTime = s_initialTime.AddHours(3);

        var entity = new SomeDomainEntity();
        context.SomeDomainEntities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        time.SetUtcNow(updateTime);
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(s_initialTime, GetCreatedAt(context, entity));
        Assert.Equal(updateTime, GetUpdatedAt(context, entity));
    }

    [Fact]
    public async Task Shadow_properties_are_default_before_SaveChanges()
    {
        // Arrange
        var time = new FakeTimeProvider(s_initialTime);
        using var factory = new TestedWebAppBuilder(testOutputHelper)
            .WithFakeTime(time)
            .Build();
        var context = factory.GetScopedService<TestDbContext>();
        var entity = new SomeAggregateRoot();

        // Act
        context.SomeEntities.Add(entity);

        // Assert
        Assert.Equal(default, GetCreatedAt(context, entity));
        Assert.Equal(default, GetUpdatedAt(context, entity));
    }

    private static DateTimeOffset GetCreatedAt(TestDbContext context, object entity)
       => (DateTimeOffset)context.Entry(entity).Property(TestDbContext.CreatedAtPropertyName).CurrentValue!;

    private static DateTimeOffset GetUpdatedAt(TestDbContext context, object entity)
        => (DateTimeOffset)context.Entry(entity).Property(TestDbContext.UpdatedAtPropertyName).CurrentValue!;
}
