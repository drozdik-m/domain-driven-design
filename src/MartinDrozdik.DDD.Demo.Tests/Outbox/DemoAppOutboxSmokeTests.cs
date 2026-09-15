using MartinDrozdik.DDD.Testing.Outbox;

namespace MartinDrozdik.DDD.Demo.Tests.Outbox;

public class DemoAppOutboxSmokeTests(ITestOutputHelper testOutputHelper)
    : OutboxSmokeTests<Program>(new DemoAppBuilder(testOutputHelper))
{
}
