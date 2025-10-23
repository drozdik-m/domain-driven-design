namespace MartinDrozdik.DDD.Demo.Tests;

public class TestDemoHostTests
{
    [Fact]
    public void Demo_host_builds_successfully()
    {
        // Arrange & Act
        var testDemoHost = TestDemoHost.CreateTest();

        // Assert
        Assert.NotNull(testDemoHost);
    }
}
