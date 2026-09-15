using MartinDrozdik.DDD.Testing.Outbox;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class TestOutboxSmokeTests(ITestOutputHelper testOutputHelper)
    : OutboxSmokeTests<Program>(new TestedWebAppBuilder(testOutputHelper))
{
}
