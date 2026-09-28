using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class TestOutboxSmokeTests(ITestOutputHelper testOutputHelper)
    : OutboxSmokeTests<Program, TestDbContext>(new TestedWebAppBuilder(testOutputHelper))
{
}
