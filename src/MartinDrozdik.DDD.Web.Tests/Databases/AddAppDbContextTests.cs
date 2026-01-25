using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static MartinDrozdik.DDD.Web.Tests.TestProgram;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

public class AddAppDbContextTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Validates_that_the_test_context_has_successful_connection()
    {
        // Arrange
        var factory = new TestAppFactory(testOutputHelper);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<TestDbContext>();

        // Act
        Assert.NotNull(dbContext);
        var canConnect = dbContext.Database.CanConnect();

        // Assert
        Assert.True(canConnect);
    }
}
