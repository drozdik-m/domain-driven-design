using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Demo.Tests.Users;

public class UsersTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Setting_testing_claims_works_correctly()
    {
        // Arrange
        const string userName = "testuser";
        string[] roles = ["admin", "user"];

        await using var factory = new DemoAppBuilder(testOutputHelper)
            .WithUserAndRoles(userName, roles)
            .Build();

        using var scope = factory.Services.CreateScope();
        var client = factory.CreateDddClient();

        // Act
        var response = await client.V1.User.Me.GetAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Multiple(
            () => Assert.Equal(userName, response.Name),
            () => Assert.Equal(userName, response.Id),
            () => Assert.Equal(roles, response.Roles));
    }
}
