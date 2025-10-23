namespace MartinDrozdik.DDD.Demo.Tests;

/// <summary>
/// Wrapper for <see cref="DemoHost"/> for testing purposes.
/// </summary>
public class TestDemoHost
{
    private DemoHost _demoHost;

    public TestDemoHost(DemoHost demoHost)
    {
        _demoHost = demoHost;
    }

    public static TestDemoHost CreateTest()
    {
        var demoHost = DemoHost.CreateDefault();
        return new TestDemoHost(demoHost);
    }
}
