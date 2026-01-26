using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.SmokeTests;

public class TestProgramSmokeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task App_returns_hello_world()
    {
        // Arrange
        var factory = new TestAppFactory(testOutputHelper);
        var client = factory.CreateClient();

        // Act
        var result = await client.GetAsync("/");

        // Assert
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();
        Assert.Equal("Hello World!", content);
    }
}
